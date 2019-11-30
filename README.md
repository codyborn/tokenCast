# TokenCast
DIY NFT Art Display


## Requirements
1. Raspberry Pi
This will be the brains of the picture frame. 4GB version
2. SD Card
We'll load the raspberry pi operating system using this SD card.
https://www.amazon.com/Sandisk-Ultra-Micro-UHS-I-Adapter/dp/B073K14CVB/ref=sr_1_3?crid=3BC3R2S3T8F6W&keywords=16gb+micro+sd+card&qid=1575080294&sprefix=16gb+micro%2Caps%2C231&sr=8-3
3. IPS Monitor
An IPS monitor is great for displaying vivid images and looks great at any angle.
4. External keyboard
A keyboard will be useful for initially configuring your raspberry pi's WIFI settings.
5. Cables
You'll need a power cable for your raspberry pi, a USB cable to power your monitor, and an HDMI to transmit video.  The IPS monitor will likely come with cables but I recommend getting some flat, angled cables to avoid bending them against the frame.
6. Frame

7. Photo mat
The mat helps . Acid free

## Setting up your Raspberry Pi

1. Install the operating system (OS) onto the SD card
https://www.raspberrypi.org/documentation/installation/installing-images/
2. Insert the SD card, keyboard, and monitor and boot up your Raspberry Pi
3. Upon boot up the OS should prompt you to configure the wifi network and install updates.
4. You can adjust the resolution by opening the start menu, select Preferences, Raspberry Pi Configuration
5. Run the following script to install TokenCast:

`bash -c "$(curl https://raw.githubusercontent.com/codyborn/tokenCast/master/install.sh)"`
6. The raspi will reboot for settings update to take effect and should automatically start tokenCast on reboot

## Connecting to your device
After running the installation script, you should be prompted with a QR code and a URL. If connecting with your phone, you can scan the QR code and navigate to the site with a web3 wallet.  If on a PC, you can navigate to the URL https://tokencast.net/Account and enter the device ID by clicking "Register New Device".

For mobile devices, we recommend using [Opera browser](https://www.opera.com/mobile) for Android or MetaMask for [Android](https://play.google.com/store/apps/details?id=io.metamask) or [IOS](http://metamask.app.link/).
For your PC, [MetaMask](https://metamask.io/) provides great browser plug-ins.

You can connect multiple devices to the same frame. When adding a new device, navigate to https://www.tokencast.net/Account and enter the same device ID. You can always find your device ID by looking in the bottom right corner of your frame.
