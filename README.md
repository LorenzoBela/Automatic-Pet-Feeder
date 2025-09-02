# Automatic Pet Feeder - Premium Edition

![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![C#](https://img.shields.io/badge/language-C%23-239120.svg)
![Windows Forms](https://img.shields.io/badge/UI-Windows%20Forms-0078d4.svg)

A sophisticated Windows Forms application for controlling an automatic pet feeder system with Arduino integration. Features a luxurious dark theme with gold accents and elegant Garamond typography.

## Features

### Core Functionality
- **Arduino Integration**: Serial communication with Arduino-based feeder hardware
- **Scheduled Feeding**: Multiple feeding interval options (hourly, daily, custom)
- **Manual Dispensing**: On-demand food dispensing with customizable duration
- **Real-time Monitoring**: Live countdown timer and current time display
- **Persistent Settings**: Automatic saving and loading of feeding schedules

### Premium Design
- **Luxurious UI**: Dark navy theme with gold accents
- **Modern Typography**: Elegant Garamond font throughout
- **Flat Design**: Clean, modern button and panel styling
- **Responsive Layout**: Professional dashboard-style interface
- **Status Indicators**: Color-coded connection and feeding status

### Technical Features
- **Serial Port Management**: Robust Arduino connection handling
- **Data Persistence**: Settings saved using application settings
- **Error Handling**: Comprehensive error management and user feedback
- **Multi-Form Architecture**: Organized code structure with separate forms

## Screenshots

### Main Dashboard
The primary interface showing connection status, feeding schedule, and real-time monitoring.

### Feeding Settings
Configure feeding times and intervals with an intuitive interface.

### Manual Dispense
Manually control food dispensing with progress tracking.

## Getting Started

### Prerequisites
- **.NET Framework 4.7.2** or higher
- **Windows 10/11** (recommended)
- **Arduino** with compatible pet feeder hardware
- **Serial Port** connection (USB/Bluetooth)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/LorenzoBela/Automatic-Pet-Feeder.git
   cd Automatic-Pet-Feeder
   ```

2. **Build the application**
   ```bash
   # Using Visual Studio
   # Open Automatic Pet Feeder.sln and build (Ctrl+Shift+B)
   
   # Or using MSBuild
   msbuild "Automatic Pet Feeder.csproj" /p:Configuration=Release
   ```

3. **Run the application**
   ```bash
   # Navigate to bin/Release and run
   "Automatic Pet Feeder.exe"
   ```

### Arduino Setup

1. **Hardware Requirements**
   - Arduino Uno/Nano/ESP32
   - Servo motor for dispensing mechanism
   - Food container and dispensing system
   - USB cable for serial communication

2. **Arduino Code**
   ```cpp
   // Basic Arduino sketch for pet feeder
   // Responds to serial commands: PING, DISPENSE_X (where X = seconds)
   
   void setup() {
     Serial.begin(9600);
     // Initialize servo and hardware
   }
   
   void loop() {
     if (Serial.available()) {
       String command = Serial.readString();
       if (command == "PING") {
         Serial.println("PONG");
       }
       // Add dispensing logic here
     }
   }
   ```

## Usage Guide

### Connecting to Arduino

1. **Connect your Arduino** via USB to your computer
2. **Click "Connect"** on the main dashboard
3. **Verify connection** - status should show "Connected" in blue
4. The application will automatically detect COM ports and attempt connection

### Setting Feeding Schedule

1. **Click "Set Feeding Time"** to open the feeding settings
2. **Choose feeding time** using the time picker (12-hour format)
3. **Select interval** from the dropdown:
   - Every Day
   - Every 2/3/4/6/8/12 Hours
   - Twice a Day
   - Three Times a Day
4. **Click "Save Settings"** to apply the schedule

### Manual Dispensing

1. **Click "Manual Dispense"** to open the manual control
2. **Set dispense time** (1-30 seconds)
3. **Click "Dispense Now"** to start feeding
4. **Monitor progress** with the progress bar

### Monitoring

- **Current Time**: Always displayed and updated every second
- **Next Feeding**: Shows countdown to next scheduled feeding
- **Connection Status**: Real-time Arduino connection status
- **Food Level**: Monitor food container status (if equipped)

## Configuration

### Feeding Intervals
| Interval | Description | Use Case |
|----------|-------------|----------|
| Every Day | Once daily at set time | Adult cats/dogs |
| Every 2-4 Hours | Frequent feeding | Puppies/kittens |
| Every 6-8 Hours | Regular intervals | Active pets |
| Twice a Day | Morning & evening | Standard feeding |
| Three Times a Day | Breakfast, lunch, dinner | Scheduled feeding |

### Serial Communication
- **Default Port**: COM3
- **Baud Rate**: 9600
- **Handshake**: PING/PONG protocol
- **Timeout**: 1 second

## Technical Architecture

### Project Structure
```
Automatic Pet Feeder/
??? Form1.cs/.Designer.cs    # Main dashboard
??? Form2.cs/.Designer.cs    # Feeding settings
??? Form3.cs/.Designer.cs    # Manual dispense
??? Program.cs               # Application entry point
??? Properties/
?   ??? Settings.settings    # Application settings
?   ??? AssemblyInfo.cs     # Assembly information
??? README.md               # This file
```

### Key Classes and Methods

#### Form1 (Main Dashboard)
- `UpdateFeedingSchedule()` - Updates feeding schedule from Form2
- `CalculateNextFeedingTime()` - Calculates next feeding based on interval
- `LoadFeedingSchedule()` / `SaveFeedingSchedule()` - Persistence methods
- `timer1_Tick()` - Real-time updates every second

#### Form2 (Feeding Settings)
- `LoadCurrentSchedule()` - Loads existing schedule when opened
- `CalculateNextFeeding()` - Calculates next feeding time
- `CalculateBaseTimeFromSchedule()` - Determines original time from schedule

#### Form3 (Manual Dispense)
- Manual feeding control interface
- Progress tracking for dispensing operation

### Data Persistence
Settings are automatically saved using .NET Application Settings:
- `NextFeedingTime` - Stores next scheduled feeding time
- `FeedingInterval` - Stores selected feeding interval

## Design System

### Color Palette
- **Primary Background**: `#17202A` (Deep Navy)
- **Panel Background**: `#2C3E50` (Charcoal Blue)
- **Gold Accent**: `#DFC27D` (Elegant Gold)
- **Text Primary**: `#FFFFFF` (White)
- **Text Secondary**: `#BDC3C7` (Light Gray)
- **Success**: `#2E86C1` (Professional Blue)
- **Warning**: `#FF9F43` (Orange)
- **Error**: `#C0392B` (Red)

### Typography
- **Font Family**: Garamond (Classic serif for elegance)
- **Title**: 28-36pt Bold
- **Headers**: 16pt Bold
- **Body Text**: 12pt Regular
- **Small Text**: 10pt Regular/Italic

## Troubleshooting

### Common Issues

**"Port COM3 not found"**
- Check Arduino connection
- Try different USB ports
- Verify Arduino is powered on
- Check Windows Device Manager

**"No device response"**
- Verify Arduino sketch is uploaded
- Check serial communication code
- Ensure baud rate matches (9600)
- Try disconnecting and reconnecting

**"Feeding schedule not saving"**
- Check application permissions
- Verify settings file access
- Run as administrator if needed

**"Connection keeps dropping"**
- Check USB cable quality
- Verify stable power supply
- Update Arduino drivers

### Debug Mode
Enable debug mode for additional logging:
1. Add `DEBUG` compilation symbol
2. Check Output window for detailed logs
3. Monitor serial communication

## Contributing

We welcome contributions! Please follow these steps:

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```
3. **Commit your changes**
   ```bash
   git commit -m 'Add amazing feature'
   ```
4. **Push to the branch**
   ```bash
   git push origin feature/amazing-feature
   ```
5. **Open a Pull Request**

### Development Guidelines
- Follow C# coding conventions
- Add XML documentation for public methods
- Test with actual Arduino hardware
- Maintain the premium design aesthetic
- Update README for new features

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Lorenzo Bela**
- GitHub: [@LorenzoBela](https://github.com/LorenzoBela)
- Repository: [Automatic-Pet-Feeder](https://github.com/LorenzoBela/Automatic-Pet-Feeder)

## Acknowledgments

- Windows Forms team for the robust UI framework
- Arduino community for hardware inspiration
- Contributors and testers
- Pet owners who inspired this project

## Support

If you encounter any issues or have questions:

1. **Check the troubleshooting section** above
2. **Search existing issues** on GitHub
3. **Create a new issue** with detailed information
4. **Include error messages** and system information

---

**Made with care for pet lovers everywhere**

[Star this repo](https://github.com/LorenzoBela/Automatic-Pet-Feeder) | [Report Bug](https://github.com/LorenzoBela/Automatic-Pet-Feeder/issues) | [Request Feature](https://github.com/LorenzoBela/Automatic-Pet-Feeder/issues)