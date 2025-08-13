// File: wwwroot/swagger-ui/custom.js
// Custom JavaScript for Swagger UI Enhancement

(function () {
    'use strict';

    // Wait for Swagger UI to load
    window.addEventListener('load', function () {

        // Add API status indicator
        setTimeout(function () {
            const statusIndicator = document.createElement('div');
            statusIndicator.className = 'api-status-indicator';
            statusIndicator.innerHTML = `
                <div class="status-dot"></div>
                <span>API Online</span>
            `;
            document.body.appendChild(statusIndicator);

            // Check API status
            checkApiStatus();
            setInterval(checkApiStatus, 30000); // Check every 30 seconds
        }, 1000);

        // Add copy button to code blocks
        document.querySelectorAll('pre').forEach(function (pre) {
            const button = document.createElement('button');
            button.className = 'copy-button';
            button.textContent = 'Copy';
            button.style.cssText = 'position: absolute; right: 10px; top: 10px; background: #667eea; color: white; border: none; padding: 5px 10px; border-radius: 4px; cursor: pointer;';

            button.addEventListener('click', function () {
                navigator.clipboard.writeText(pre.textContent).then(function () {
                    button.textContent = 'Copied!';
                    setTimeout(function () {
                        button.textContent = 'Copy';
                    }, 2000);
                });
            });

            pre.style.position = 'relative';
            pre.appendChild(button);
        });

        // Add search functionality enhancement
        enhanceSearch();

        // Add keyboard shortcuts
        addKeyboardShortcuts();

        // Add request/response time tracking
        trackRequestTimes();
    });

    function checkApiStatus() {
        fetch('/api/site/status-summary', {
            method: 'GET',
            headers: {
                'Authorization': 'Bearer ' + getStoredToken()
            }
        })
            .then(response => {
                const indicator = document.querySelector('.api-status-indicator');
                if (indicator) {
                    if (response.ok) {
                        indicator.querySelector('.status-dot').style.background = '#49cc90';
                        indicator.querySelector('span').textContent = 'API Online';
                    } else if (response.status === 401) {
                        indicator.querySelector('.status-dot').style.background = '#fca130';
                        indicator.querySelector('span').textContent = 'Not Authenticated';
                    } else {
                        indicator.querySelector('.status-dot').style.background = '#f93e3e';
                        indicator.querySelector('span').textContent = 'API Error';
                    }
                }
            })
            .catch(error => {
                const indicator = document.querySelector('.api-status-indicator');
                if (indicator) {
                    indicator.querySelector('.status-dot').style.background = '#f93e3e';
                    indicator.querySelector('span').textContent = 'API Offline';
                }
            });
    }

    function getStoredToken() {
        // Try to get token from Swagger UI's authorization
        const authWrapper = document.querySelector('.auth-wrapper');
        if (authWrapper) {
            const authValue = authWrapper.querySelector('input[type="text"]');
            if (authValue && authValue.value) {
                return authValue.value.replace('Bearer ', '');
            }
        }
        return '';
    }

    function enhanceSearch() {
        // Add search box if not present
        const topbar = document.querySelector('.swagger-ui .topbar');
        if (topbar && !document.querySelector('.api-search')) {
            const searchBox = document.createElement('input');
            searchBox.type = 'text';
            searchBox.placeholder = 'Search endpoints...';
            searchBox.className = 'api-search';
            searchBox.style.cssText = 'padding: 8px; border-radius: 4px; border: 1px solid #ddd; width: 300px; margin-left: 20px;';

            searchBox.addEventListener('input', function (e) {
                const searchTerm = e.target.value.toLowerCase();
                document.querySelectorAll('.opblock').forEach(function (block) {
                    const text = block.textContent.toLowerCase();
                    block.style.display = text.includes(searchTerm) ? 'block' : 'none';
                });
            });

            topbar.appendChild(searchBox);
        }
    }

    function addKeyboardShortcuts() {
        document.addEventListener('keydown', function (e) {
            // Ctrl/Cmd + K for search focus
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                const searchBox = document.querySelector('.api-search');
                if (searchBox) searchBox.focus();
            }

            // Ctrl/Cmd + E to expand all
            if ((e.ctrlKey || e.metaKey) && e.key === 'e') {
                e.preventDefault();
                document.querySelectorAll('.opblock-summary').forEach(function (summary) {
                    if (!summary.parentElement.classList.contains('is-open')) {
                        summary.click();
                    }
                });
            }

            // Ctrl/Cmd + W to collapse all
            if ((e.ctrlKey || e.metaKey) && e.key === 'w') {
                e.preventDefault();
                document.querySelectorAll('.opblock-summary').forEach(function (summary) {
                    if (summary.parentElement.classList.contains('is-open')) {
                        summary.click();
                    }
                });
            }
        });
    }

    function trackRequestTimes() {
        // Override fetch to track request times
        const originalFetch = window.fetch;
        window.fetch = function (...args) {
            const startTime = performance.now();
            return originalFetch.apply(this, args).then(response => {
                const endTime = performance.now();
                const duration = Math.round(endTime - startTime);
                console.log(`API Request: ${args[0]} - ${duration}ms`);
                return response;
            });
        };
    }

})();