/**
 * Theme Manager - Handles dark/light mode toggling
 */
class ThemeManager {
    constructor() {
        this.currentTheme = this.getStoredTheme() || this.getSystemPreference();
        this.themeToggleBtn = null;
        this.themeIcon = null;
    }

    /**
     * Initialize the theme manager
     */
    init() {
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                this.applyTheme(this.currentTheme);
                this.setupEventListeners();
            });
        } else {
            this.applyTheme(this.currentTheme);
            this.setupEventListeners();
        }

        // Listen for system theme changes
        this.watchSystemTheme();
    }

    /**
     * Set up event listeners
     */
    setupEventListeners() {
        this.themeToggleBtn = document.getElementById('themeToggleBtn');
        this.themeIcon = document.querySelector('.theme-icon');

        if (this.themeToggleBtn) {
            this.themeToggleBtn.addEventListener('click', () => this.toggleTheme());

            // Keyboard shortcut: Ctrl+Shift+T
            document.addEventListener('keydown', (e) => {
                if (e.ctrlKey && e.shiftKey && e.key === 'T') {
                    e.preventDefault();
                    this.toggleTheme();
                }
            });

            // Update icon to match current theme
            this.updateIcon();
        }
    }

    /**
     * Get stored theme preference from localStorage
     */
    getStoredTheme() {
        return localStorage.getItem('workflow-theme');
    }

    /**
     * Get system color scheme preference
     */
    getSystemPreference() {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }

    /**
     * Watch for system theme changes
     */
    watchSystemTheme() {
        if (window.matchMedia) {
            const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');

            // Modern browsers
            if (darkModeQuery.addEventListener) {
                darkModeQuery.addEventListener('change', (e) => {
                    // Only auto-switch if user hasn't set a preference
                    if (!this.getStoredTheme()) {
                        this.applyTheme(e.matches ? 'dark' : 'light');
                    }
                });
            }
            // Legacy browsers
            else if (darkModeQuery.addListener) {
                darkModeQuery.addListener((e) => {
                    if (!this.getStoredTheme()) {
                        this.applyTheme(e.matches ? 'dark' : 'light');
                    }
                });
            }
        }
    }

    /**
     * Toggle between light and dark themes
     */
    toggleTheme() {
        const newTheme = this.currentTheme === 'dark' ? 'light' : 'dark';
        this.applyTheme(newTheme);
        this.saveTheme(newTheme);

        // Add a subtle animation to the button
        if (this.themeToggleBtn) {
            this.themeToggleBtn.style.transform = 'scale(0.9)';
            setTimeout(() => {
                this.themeToggleBtn.style.transform = 'scale(1)';
            }, 150);
        }
    }

    /**
     * Apply the theme to the document
     * @param {string} theme - 'light' or 'dark'
     */
    applyTheme(theme) {
        this.currentTheme = theme;

        // Ensure body element exists
        if (!document.body) {
            console.warn('Body element not available yet, deferring theme application');
            return;
        }

        if (theme === 'dark') {
            document.body.classList.add('dark-mode');
        } else {
            document.body.classList.remove('dark-mode');
        }

        this.updateIcon();
        this.updateCanvasBackground();

        // Dispatch custom event for other components to listen to
        window.dispatchEvent(new CustomEvent('themechange', {
            detail: { theme }
        }));
    }

    /**
     * Update the theme toggle button icon
     */
    updateIcon() {
        if (this.themeIcon) {
            // Use moon for light mode (clicking switches to dark)
            // Use sun for dark mode (clicking switches to light)
            this.themeIcon.textContent = this.currentTheme === 'dark' ? '☀️' : '🌙';

            if (this.themeToggleBtn) {
                this.themeToggleBtn.title = this.currentTheme === 'dark'
                    ? 'Switch to Light Mode (Ctrl+Shift+T)'
                    : 'Switch to Dark Mode (Ctrl+Shift+T)';
            }
        }
    }

    /**
     * Update canvas background when theme changes
     */
    updateCanvasBackground() {
        // The canvas will be updated via the 'themechange' event
        // which is dispatched in toggleTheme() and applyTheme()
        // WorkflowCanvas listens to this event and calls its updateTheme() method
        // No direct call needed here
    }

    /**
     * Save theme preference to localStorage
     * @param {string} theme - 'light' or 'dark'
     */
    saveTheme(theme) {
        localStorage.setItem('workflow-theme', theme);
    }

    /**
     * Get current theme
     */
    getTheme() {
        return this.currentTheme;
    }

    /**
     * Set theme programmatically
     * @param {string} theme - 'light' or 'dark'
     */
    setTheme(theme) {
        if (theme === 'light' || theme === 'dark') {
            this.applyTheme(theme);
            this.saveTheme(theme);
        }
    }

    /**
     * Reset to system preference
     */
    resetToSystemPreference() {
        localStorage.removeItem('workflow-theme');
        const systemTheme = this.getSystemPreference();
        this.applyTheme(systemTheme);
    }
}

// Initialize theme manager immediately (before DOM loads to prevent flash)
const themeManager = new ThemeManager();
themeManager.init();

// Export for use in other modules
window.themeManager = themeManager;
