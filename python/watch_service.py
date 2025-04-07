import os
import time
import threading
from datetime import datetime
import uuid
from PIL import Image
import logging


class WatchService:
    def __init__(self, config, image_classifier, camera_feed_service, slack_notifier):
        self.image_classifier = image_classifier
        self.camera_feed_service = camera_feed_service
        self.slack_notifier = slack_notifier
        
        self.status_file_path = config['WatchService']['status_file_path']
        self.interval_seconds = int(config['WatchService']['interval_seconds'])
        self.original_photo_save_path = config['WatchService']['original_photo_save_path']
        self.cropped_photo_save_path = config['WatchService']['cropped_photo_save_path']
        
        self.crop_start_x = int(config['CameraFeed']['crop_start_x'])
        self.crop_start_y = int(config['CameraFeed']['crop_start_y'])
        self.width = int(config['CameraFeed']['width'])
        self.height = int(config['CameraFeed']['height'])
        
        self.successful = 0
        self.failed = 0
        self.timer = None
        self.running = False
        
        print("Initializing WatchService")

    def start(self):
        """Start the watch service with periodic checks"""
        self.running = True
        self.on_timer_tick()
    
    def stop(self):
        """Stop the watch service"""
        self.running = False
        if self.timer:
            self.timer.cancel()
    
    def on_timer_tick(self):
        """Process image from camera feed and classify it"""
        print("Processing image")
        start_time = time.time()
        
        full_image_path = None
        cropped_image_path = None
        
        try:
            full_image_path = self.camera_feed_service.get_photo()
            print("Got photo")
            
            current_status = None
            if os.path.exists(self.status_file_path):
                with open(self.status_file_path, 'r') as f:
                    current_status = f.read().strip()
            
            cropped_image_path = self.get_cropped_image_path(full_image_path)
            
            print("About to classify image")
            new_status = self.image_classifier.classify_image(cropped_image_path)
            
            print(f"New status: {new_status}")
            if current_status != new_status:
                # Uncomment to enable Slack notifications
                # self.slack_notifier.send_notification(
                #     f"Status changed from {current_status} to {new_status}",
                #     full_image_path
                # )
                
                # Update status file
                with open(self.status_file_path, 'w') as f:
                    f.write(new_status)
            
            self.successful += 1
            
            if self.successful == 1:
                print("Successfully processed the first image")
                
        except Exception as e:
            self.failed += 1
            logging.error(f"Error getting image and classifying it: {str(e)}", exc_info=True)
            
        finally:
            # Calculate next run time
            now = time.time()
            next_run_time = start_time + self.interval_seconds
            delay = max(0, next_run_time - now)
            
            self.safe_delete_file(full_image_path, self.original_photo_save_path)
            self.safe_delete_file(cropped_image_path, self.cropped_photo_save_path)
            
            print("Finished processing image")
            
            # Schedule next run if still running
            if self.running:
                self.timer = threading.Timer(delay, self.on_timer_tick)
                self.timer.daemon = True
                self.timer.start()
    
    def get_stats(self):
        """Return statistics about successful and failed attempts"""
        return (self.successful, self.failed)
    
    def safe_delete_file(self, file_path, save_path):
        """Save a copy of the file if needed, then delete the original"""
        try:
            if file_path and save_path:
                # Save the file if a save path is specified
                os.makedirs(save_path, exist_ok=True)
                timestamp = datetime.now().strftime("%Y%m%d_%H%M%S_%f")
                filename = f"{timestamp}.jpg"
                
                if os.path.exists(file_path):
                    new_path = os.path.join(save_path, filename)
                    try:
                        os.makedirs(os.path.dirname(new_path), exist_ok=True)
                        with open(file_path, 'rb') as src, open(new_path, 'wb') as dst:
                            dst.write(src.read())
                    except Exception as e:
                        logging.error(f"Failed to save image {file_path} to {save_path}: {str(e)}")
            
            # Delete the original file
            if file_path and os.path.exists(file_path):
                os.remove(file_path)
                
        except Exception as e:
            logging.error(f"Failed to delete temp file {file_path}: {str(e)}")
    
    def get_cropped_image_path(self, image_path):
        """Crop the image and save it to a temporary file"""
        from tempfile import gettempdir
        
        temp_path = os.path.join(gettempdir(), f"{uuid.uuid4()}.jpg")
        
        with Image.open(image_path) as img:
            cropped_img = img.crop((
                self.crop_start_x,
                self.crop_start_y,
                self.crop_start_x + self.width,
                self.crop_start_y + self.height
            ))
            cropped_img.save(temp_path)
            
        return temp_path