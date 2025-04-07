import os
import sys
import logging
import configparser
import argparse
from datetime import datetime

from Services.camera_feed import CameraFeedService
from Services.image_classifier import ImageClassifier
from Services.slack_notifier import SlackNotifier
from Services.watch_service import WatchService


def main():
    # Set up argument parser
    parser = argparse.ArgumentParser(description="Camera Notifier")
    parser.add_argument("--config", help="Path to custom config file", default="config.ini")
    args = parser.parse_args()
    
    # Set up logging
    log_dir = "logs"
    os.makedirs(log_dir, exist_ok=True)
    log_file = os.path.join(log_dir, f"camera_notifier_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log")
    
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler(log_file),
            logging.StreamHandler(sys.stdout)
        ]
    )
    
    logger = logging.getLogger(__name__)
    logger.info("Starting Camera Notifier application")
    
    # Load configuration
    config = configparser.ConfigParser()
    config_file = args.config
    
    if not os.path.exists(config_file):
        logger.error(f"Config file not found: {config_file}")
        sys.exit(1)
    
    config.read(config_file)
    logger.info(f"Loaded configuration from {config_file}")
    
    try:
        # Initialize services
        camera_feed_service = CameraFeedService(config)
        image_classifier = ImageClassifier(config)
        slack_notifier = SlackNotifier(config)
        
        # Initialize and start watch service
        watch_service = WatchService(config, image_classifier, camera_feed_service, slack_notifier)
        watch_service.start()
        
        # Keep application running
        try:
            logger.info("Application running, press Ctrl+C to exit")
            while True:
                # Main thread just waits
                stats = watch_service.get_stats()
                logger.info(f"Stats - Successful: {stats[0]}, Failed: {stats[1]}")
                
                # Sleep for a bit before reporting stats again
                import time
                time.sleep(300)  # 5 minutes
                
        except KeyboardInterrupt:
            logger.info("Shutting down application")
            watch_service.stop()
    
    except Exception as e:
        logger.exception("Unhandled exception in main application")
        sys.exit(1)


if __name__ == "__main__":
    main()
    