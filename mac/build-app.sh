#!/bin/bash
# Builds 677777.app (Apple silicon + Intel), ad-hoc signs it, and zips it to mac/dist/677777-mac.zip.
set -euo pipefail
cd "$(dirname "$0")"
VERSION="${VERSION:-1.0.0}"
APP="dist/677777.app"

rm -rf dist
mkdir -p "$APP/Contents/MacOS"
for arch in arm64 x86_64; do
  swiftc -O -target "$arch-apple-macos13.0" main.swift -o "dist/SixSeven-$arch"
done
lipo -create dist/SixSeven-arm64 dist/SixSeven-x86_64 -output "$APP/Contents/MacOS/SixSeven"
rm dist/SixSeven-arm64 dist/SixSeven-x86_64

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>677777</string>
  <key>CFBundleIdentifier</key><string>io.github.techbobkl-web.SixSeven</string>
  <key>CFBundleExecutable</key><string>SixSeven</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>${VERSION}</string>
  <key>CFBundleVersion</key><string>${VERSION}</string>
  <key>LSMinimumSystemVersion</key><string>13.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST

codesign --force --sign - --timestamp=none "$APP"
ditto -c -k --keepParent "$APP" dist/677777-mac.zip
echo "Built $APP and dist/677777-mac.zip"
