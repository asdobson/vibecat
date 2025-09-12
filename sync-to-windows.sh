#!/bin/bash
# Sync VibeCat project to Windows C: drive

echo "Syncing VibeCat to C:\\vibecat..."
rsync -av --delete /home/adobson/source/repos/vibecat/VibeCat/ /mnt/c/vibecat/
echo "Sync complete! Project available at C:\\vibecat"