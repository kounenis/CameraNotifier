import os
import torch
from torchvision import models, transforms
from torch.utils.data import DataLoader, Dataset
import torch.nn as nn
from PIL import Image
import numpy as np


class ImageDataset(Dataset):
    def __init__(self, root_dir, transform=None):
        self.root_dir = root_dir
        self.transform = transform
        self.classes = sorted([d for d in os.listdir(root_dir) 
                              if os.path.isdir(os.path.join(root_dir, d))])
        self.class_to_idx = {cls_name: i for i, cls_name in enumerate(self.classes)}
        
        self.samples = []
        for class_name in self.classes:
            class_dir = os.path.join(root_dir, class_name)
            for img_name in os.listdir(class_dir):
                img_path = os.path.join(class_dir, img_name)
                if os.path.isfile(img_path):
                    self.samples.append((img_path, self.class_to_idx[class_name]))
    
    def __len__(self):
        return len(self.samples)
    
    def __getitem__(self, idx):
        img_path, label = self.samples[idx]
        image = Image.open(img_path).convert('RGB')
        
        if self.transform:
            image = self.transform(image)
            
        return image, label


class ImageClassifier:
    def __init__(self, config):
        self.model_path = config['ImageClassifier']['model_path']
        self.training_path = config['ImageClassifier']['training_path']
        self.model = None
        self.class_names = None
        self.device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")
        self.transform = transforms.Compose([
            transforms.Resize(256),
            transforms.CenterCrop(224),
            transforms.ToTensor(),
            transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
        ])
        print(f"Initializing ImageClassifier with device: {self.device}")

    def classify_image(self, image_path):
        """Classify an image and return the predicted label"""
        if self.model is None:
            self.build_model(False)
            
        # Load and preprocess the image
        image = Image.open(image_path).convert('RGB')
        image_tensor = self.transform(image)
        image_tensor = image_tensor.unsqueeze(0)  # Add batch dimension
        
        # Move to device (CPU or GPU)
        image_tensor = image_tensor.to(self.device)
        
        # Set model to evaluation mode
        self.model.eval()
        
        # Make prediction
        with torch.no_grad():
            outputs = self.model(image_tensor)
            _, predicted = torch.max(outputs, 1)
            
        return self.class_names[predicted.item()]

    def build_model(self, clear_previous=False):
        """Build or load the classification model"""
        # Get class names from training directories
        self.class_names = sorted([d for d in os.listdir(self.training_path) 
                            if os.path.isdir(os.path.join(self.training_path, d))])
        
        if os.path.exists(self.model_path) and not clear_previous:
            print("Loading existing model")
            self.model = models.resnet50(pretrained=False)
            num_ftrs = self.model.fc.in_features
            self.model.fc = nn.Linear(num_ftrs, len(self.class_names))
            self.model.load_state_dict(torch.load(self.model_path, map_location=self.device))
            self.model = self.model.to(self.device)
        else:
            print("Creating new model")
            self.model = self.create_model()
            
        self.model.eval()
        print("Model is ready")

    def create_model(self):
        """Create and train a new image classification model"""
        # Ensure model directory exists
        os.makedirs(os.path.dirname(self.model_path), exist_ok=True)
        
        # Data transformations for training
        train_transform = transforms.Compose([
            transforms.RandomResizedCrop(224),
            transforms.RandomHorizontalFlip(),
            transforms.ToTensor(),
            transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
        ])
        
        # Create dataset and dataloader
        train_dataset = ImageDataset(self.training_path, transform=train_transform)
        
        # Split into train/validation
        train_size = int(0.8 * len(train_dataset))
        valid_size = len(train_dataset) - train_size
        train_dataset, valid_dataset = torch.utils.data.random_split(
            train_dataset, [train_size, valid_size]
        )
        
        train_loader = DataLoader(train_dataset, batch_size=32, shuffle=True, num_workers=4)
        valid_loader = DataLoader(valid_dataset, batch_size=32, shuffle=False, num_workers=4)
        
        # Create the model - using ResNet50 as the base
        model = models.resnet50(pretrained=True)
        
        # Replace the final fully connected layer
        num_ftrs = model.fc.in_features
        model.fc = nn.Linear(num_ftrs, len(self.class_names))
        
        # Move model to device
        model = model.to(self.device)
        
        # Loss function and optimizer
        criterion = nn.CrossEntropyLoss()
        optimizer = torch.optim.Adam(model.parameters(), lr=0.001)
        
        # Train the model
        num_epochs = 10
        best_acc = 0.0
        
        for epoch in range(num_epochs):
            # Training phase
            model.train()
            running_loss = 0.0
            for inputs, labels in train_loader:
                inputs = inputs.to(self.device)
                labels = labels.to(self.device)
                
                optimizer.zero_grad()
                outputs = model(inputs)
                loss = criterion(outputs, labels)
                loss.backward()
                optimizer.step()
                
                running_loss += loss.item() * inputs.size(0)
            
            epoch_loss = running_loss / len(train_dataset)
            
            # Validation phase
            model.eval()
            correct = 0
            total = 0
            with torch.no_grad():
                for inputs, labels in valid_loader:
                    inputs = inputs.to(self.device)
                    labels = labels.to(self.device)
                    
                    outputs = model(inputs)
                    _, predicted = torch.max(outputs, 1)
                    
                    total += labels.size(0)
                    correct += (predicted == labels).sum().item()
            
            val_acc = correct / total
            print(f'Epoch {epoch+1}/{num_epochs}, Loss: {epoch_loss:.4f}, Validation Accuracy: {val_acc:.4f}')
            
            # Save best model
            if val_acc > best_acc:
                best_acc = val_acc
                torch.save(model.state_dict(), self.model_path)
                print(f'Model saved with validation accuracy: {best_acc:.4f}')
        
        # Load the best model
        model.load_state_dict(torch.load(self.model_path))
        return model 