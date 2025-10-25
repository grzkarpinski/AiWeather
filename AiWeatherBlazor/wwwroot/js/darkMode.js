window.darkModeHelper = {
    initialize: function() {
        const isDarkMode = localStorage.getItem('darkMode') === 'true';
        if (isDarkMode) {
            document.body.classList.add('dark-mode');
        }
        return isDarkMode;
    },
    toggle: function() {
        const isDarkMode = document.body.classList.toggle('dark-mode');
        localStorage.setItem('darkMode', isDarkMode);
        return isDarkMode;
    },
    isDarkMode: function() {
        return document.body.classList.contains('dark-mode');
    }
};

// Initialize dark mode on page load
window.darkModeHelper.initialize();
