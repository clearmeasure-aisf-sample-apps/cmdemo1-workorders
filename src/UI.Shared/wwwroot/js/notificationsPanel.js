export function setupEscapeHandler(dotNetRef) {
    const handleKeyDown = (e) => {
        if (e.key === 'Escape') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('HandleEscapeKey');
        }
    };

    document.addEventListener('keydown', handleKeyDown);
    window.notificationsPanelKeydownHandler = handleKeyDown;
}

export function cleanupEscapeHandler() {
    if (window.notificationsPanelKeydownHandler) {
        document.removeEventListener('keydown', window.notificationsPanelKeydownHandler);
        delete window.notificationsPanelKeydownHandler;
    }
}
