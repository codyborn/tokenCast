#!/usr/bin/env bash

# install chromium
sudo apt update
sudo apt-get install chromium-browser --yes

# hide cursor
sudo apt-get install unclutter
sudo echo "@unclutter -idle 0.1" >> /etc/rc.local

# Prevent raspi from sleeping
sudo echo "[SeatDefaults]
xserver-command=X -s 0 -dpms" >> /etc/lightdm/lightdm.conf

# Remove temperature warning icon
sudo echo "avoid_warnings=1" >> /boot/config.txt

# generate unique id
sudo apt-get install --reinstall wamerican
frameid=$(shuf -n4 /usr/share/dict/words | tr '\n' '_' | tr -d "'" | tr '[:upper:]' '[:lower:]')
frameid=$(echo ${frameid%?})
rm frameid.txt
echo $frameid >> frameid.txt

# set chromium to open page on start up
mkdir /home/pi/.config/autostart

echo "[Desktop Entry]
Type=Application
Name=TokenCast

# Execute chromium in Kiosk mode:
Exec=chromium-browser --kiosk --app=https://tokencast.net/device?deviceId=$frameid" >> /home/pi/.config/autostart/tokencast.desktop

# If you don't want kiosk mode, select full-screen and comment the previews command
# Full-screen mode: @chromium-browser --start-fullscreen. The browser starts in app. mode but expanded to full-screen. You can press the F11 key to break out of full-screen mode.
# Kiosk mode: @chromium-browser --kiosk. The browser starts in full-screen mode but does not respond to the F11 key, nor to any command to get it to switch tasks.
 
# Exec=chromium-browser --start-fullscreen --app=https://tokencast.net/device?deviceId=$frameid" >> /home/pi/.config/autostart/tokencast.desktop

# reboot to have the xserver-command take effect
sudo reboot
