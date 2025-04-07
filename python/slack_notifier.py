from slack_sdk import WebClient
from slack_sdk.errors import SlackApiError
import os


class SlackNotifier:
    def __init__(self, config):
        self.api_key = config['SlackNotifier']['api_key']
        self.channel = config['SlackNotifier']['channel']
        self.client = WebClient(token=self.api_key)
        print("Initializing SlackNotifier")

    def send_notification(self, text, image_file_path):
        """Send a text notification with an image attachment to Slack"""
        try:
            # Upload file
            response = self.client.files_upload(
                channels=self.channel,
                file=image_file_path,
                initial_comment=text
            )
            
            return response
        except SlackApiError as e:
            print(f"Failed to send Slack notification: {e.response['error']}")