// 677777 — a harmless prank for macOS.
//
// Pops up little Roblox-launcher-style windows all over the screen and fills them with "677777" as if someone
// were pasting it over and over, until you press Shift+1 (works even when another app is in
// front). ⌘Q and Force Quit (⌥⌘Esc) also stop it. It only draws its own windows: it doesn't
// type into other apps, touch the clipboard, start at login, or change any files, and it never
// has more than 40 windows open.
//
// Usage: 677777.app   (the binary also takes --seconds N to stop by itself, handy for testing)

import AppKit
import Carbon.HIToolbox

private let spamText = "677777 "
private let maxWindows = 150
private let newWindowEvery: TimeInterval = 0.15
private let pasteEvery: TimeInterval = 0.04

final class PrankController: NSObject, NSApplicationDelegate {
    private var popups: [(window: NSWindow, text: NSTextView)] = []
    private var timers: [Timer] = []
    private var hotKey: EventHotKeyRef?
    private let stopAfter: TimeInterval?

    init(stopAfter: TimeInterval?) {
        self.stopAfter = stopAfter
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.regular)
        NSApp.activate(ignoringOtherApps: true)
        registerStopKey()

        timers.append(Timer.scheduledTimer(withTimeInterval: newWindowEvery, repeats: true) { [weak self] _ in
            self?.openPopup()
        })
        timers.append(Timer.scheduledTimer(withTimeInterval: pasteEvery, repeats: true) { [weak self] _ in
            self?.paste()
        })
        if let stopAfter {
            timers.append(Timer.scheduledTimer(withTimeInterval: stopAfter, repeats: false) { _ in
                NSApp.terminate(nil)
            })
        }

        openPopup()
    }

    func applicationWillTerminate(_ notification: Notification) {
        if let hotKey { UnregisterEventHotKey(hotKey) }
    }

    /// Shift+1 stops it from anywhere — a real system hot key (RegisterEventHotKey), which only
    /// fires on an actual Shift+1 press, so the prank never stops itself. ⌘Q still quits too. The
    /// local monitor just swallows other typing so you can't type into the fake windows; it does
    /// not stop the prank (a local keyDown monitor is too easy to trip, so the hot key owns Shift+1).
    private func registerStopKey() {
        var pressed = EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyPressed))
        InstallEventHandler(GetApplicationEventTarget(), { _, _, _ in
            NSApp.terminate(nil)
            return noErr
        }, 1, &pressed, nil, nil)
        RegisterEventHotKey(UInt32(kVK_ANSI_1), UInt32(shiftKey), EventHotKeyID(signature: OSType(0x3637_3737), id: 1),
                            GetApplicationEventTarget(), 0, &hotKey)

        NSEvent.addLocalMonitorForEvents(matching: .keyDown) { event in
            // Let ⌘-shortcuts (⌘Q) through; swallow ordinary typing.
            event.modifierFlags.contains(.command) ? event : nil
        }
    }

    private func openPopup() {
        if popups.count >= maxWindows {
            popups.removeFirst().window.close()
        }

        let screen = NSScreen.screens.randomElement() ?? NSScreen.main!
        let area = screen.visibleFrame
        let size = NSSize(width: .random(in: 260...420), height: .random(in: 150...260))
        let origin = NSPoint(
            x: area.minX + .random(in: 0...max(1, area.width - size.width)),
            y: area.minY + .random(in: 0...max(1, area.height - size.height)))

        let window = NSWindow(
            contentRect: NSRect(origin: origin, size: size),
            styleMask: [.titled, .closable, .miniaturizable],
            backing: .buffered,
            defer: false)
        window.isReleasedWhenClosed = false
        window.title = "Roblox"
        window.level = .floating

        // Dark "Roblox is starting" look. One scroll view fills the window (this form is stable
        // even with a lot of windows open); the "Starting Roblox…" header is the first line of text.
        let scroll = NSTextView.scrollableTextView()
        let text = scroll.documentView as! NSTextView
        text.isEditable = false
        text.isSelectable = false
        text.font = .monospacedSystemFont(ofSize: .random(in: 14...24), weight: .bold)
        text.textColor = NSColor(hue: .random(in: 0...1), saturation: 0.7, brightness: 1, alpha: 1)
        text.backgroundColor = NSColor(red: 0.08, green: 0.09, blue: 0.11, alpha: 1)
        text.textContainerInset = NSSize(width: 6, height: 8)

        let header = NSAttributedString(
            string: "⬤  Starting Roblox 677777…\n\n",
            attributes: [
                .font: NSFont.systemFont(ofSize: 17, weight: .bold),
                .foregroundColor: NSColor.white,
            ])
        text.textStorage?.append(header)

        window.contentView = scroll

        window.makeKeyAndOrderFront(nil)
        popups.append((window, text))
    }

    private func paste() {
        for popup in popups {
            guard let storage = popup.text.textStorage else { continue }
            if storage.length > 6000 {
                storage.deleteCharacters(in: NSRange(location: 0, length: 3000))
            }
            let attributes: [NSAttributedString.Key: Any] = [
                .font: popup.text.font as Any,
                .foregroundColor: popup.text.textColor as Any,
            ]
            storage.append(NSAttributedString(string: String(repeating: spamText, count: .random(in: 1...3)), attributes: attributes))
            popup.text.scrollToEndOfDocument(nil)
        }
    }
}

let arguments = CommandLine.arguments
var stopAfter: TimeInterval?
if let flag = arguments.firstIndex(of: "--seconds"), flag + 1 < arguments.count, let value = TimeInterval(arguments[flag + 1]) {
    stopAfter = max(value, 1)
}

let app = NSApplication.shared
let controller = PrankController(stopAfter: stopAfter)
app.delegate = controller
app.run()
