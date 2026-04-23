const SNAPSHOT_KEY = 'agentic-pa-session-v1';

window.downloadText = function (filename, text) {
    const blob = new Blob([text], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.loadSessionSnapshot = function () {
    try {
        return window.localStorage.getItem(SNAPSHOT_KEY);
    } catch (e) {
        console.warn('loadSessionSnapshot failed', e);
        return null;
    }
};

window.saveSessionSnapshot = function (json) {
    try {
        if (json == null || json === '') {
            window.localStorage.removeItem(SNAPSHOT_KEY);
        } else {
            window.localStorage.setItem(SNAPSHOT_KEY, json);
        }
    } catch (e) {
        console.warn('saveSessionSnapshot failed', e);
    }
};

window.clearSessionSnapshot = function () {
    try { window.localStorage.removeItem(SNAPSHOT_KEY); }
    catch (e) { console.warn('clearSessionSnapshot failed', e); }
};

window.scrollToBottom = function (el) {
    if (!el) return;
    try {
        el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
    } catch (e) {
        el.scrollTop = el.scrollHeight;
    }
};
