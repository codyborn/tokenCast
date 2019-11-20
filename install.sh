#!/usr/bin/env bash

# install chromium
sudo apt update
sudo apt-get install chromium-browser --yes

# generate unique id
sudo apt-get install --reinstall wamerican
frameid=$(shuf -n4 /usr/share/dict/words | tr '\n' ' ' | tr -d "'s" | tr '[:upper:]' '[:lower:]')
echo $frameid >> frameid.txt

# set chromium to open page on start up
sudo sed -i -e '$i chromium-browser --start-fullscreen https://tokencast.net/device?deviceId=test &\' /etc/rc.local
sudo chromium-browser --start-fullscreen https://tokencast.net/device?deviceId=test