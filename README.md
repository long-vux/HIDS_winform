# Host-Based Intrusion Detection System (HIDS)

## Introduction

The Host-Based Intrusion Detection System (HIDS) is a WinForms application designed to monitor file changes in the system, track system metrics such as CPU and RAM usage, and send alerts when abnormal behaviors are detected. This application helps protect the security of important files and provides timely information to administrators.

## Features

- **File Change Monitoring**: Tracks changes, creation, deletion, and renaming of files in a specific directory.
- **System Metrics Monitoring**: Monitors CPU and RAM usage in real-time.
- **Email Alerts**: Sends notifications via email when abnormal behaviors are detected, such as the deletion of important files.
- **Logging**: Records events and alerts into a log file for later review and analysis.

## Requirements

- .NET Framework 4.5 or higher
- Visual Studio (or similar IDE) for development and compilation
- `System.Net.Mail` library for sending emails

## Installation

1. **Clone or download the project**:
   ```bash
   git clone https://github.com/long-vux/HIDS_winform.git
  
2. **Open the project in Visual Studio**:
   - Open the `.sln` file in Visual Studio.

3. **Install necessary libraries**:
   - Ensure that you have installed all required libraries.

4. **Edit email information**:
   - Open the source code file and update the email information in the `SendEmailNotification` method with your email address and login credentials.

5. **Run the application**:
   - Press `F5` to build and run the application.

## Usage

- Enter the directory path you want to monitor in the input field.
- Click the "Start" button to begin monitoring.
- Click the "Stop" button to stop monitoring.
- Monitor file changes and system metrics in the user interface.

## Alerts

- Ensure that the application has access rights to the directories you want to monitor.
- Check the email settings to ensure that notifications are sent successfully.

## Future Enhancements

- Improve the detection of abnormal behaviors using machine learning algorithms.
- Integrate with Security Information and Event Management (SIEM) systems.
- Add a more user-friendly interface and customizable features.

## Contact

If you have any questions, please contact me via email: hoanglongvu233@gmail.com.
