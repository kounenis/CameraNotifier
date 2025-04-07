import os
import uuid
import requests
from tempfile import gettempdir


class CameraFeedService:
    def __init__(self, config):
        self.url = config['CameraFeed']['url']
        self.username = config['CameraFeed']['username']
        self.password = config['CameraFeed']['password']
        print("Initializing CameraFeedService")

    def get_photo(self):
        """Downloads a photo from the camera and returns the path to the temporary file"""
        temp_file = os.path.join(gettempdir(), f"{uuid.uuid4()}.jpg")
        
        response = requests.get(
            self.url,
            auth=(self.username, self.password),
            stream=True
        )
        
        if response.status_code == 200:
            with open(temp_file, 'wb') as f:
                for chunk in response.iter_content(chunk_size=1024):
                    f.write(chunk)
            return temp_file
        else:
            raise Exception(f"Failed to download image: {response.status_code}")