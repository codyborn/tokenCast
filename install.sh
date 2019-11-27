#!/usr/bin/env bash

# install chromium
sudo apt update
sudo apt-get install chromium-browser --yes

# hide cursor
sudo apt-get install unclutter
sudo echo "@unclutter -idle 0.1" >> /etc/rc.local

# generate unique id
sudo apt-get install --reinstall wamerican
frameid=$(shuf -n4 /usr/share/dict/words | tr '\n' ' ' | tr -d "'s" | tr '[:upper:]' '[:lower:]')
echo $frameid >> frameid.txt

# set chromium to open page on start up
mkdir /home/pi/.config/autostart

echo "[Desktop Entry]
Type=Application
Name=TokenCast
Exec=chromium-browser --start-fullscreen https://tokencast.net/device?deviceId=$frameid" >> /home/pi/.config/autostart/tokencast.desktop


chromium-browser --start-fullscreen https://tokencast.net/device?deviceId=$frameid

