// Site JavaScript
function navigateTo(url) {
    // Add a loading state to the body
    document.body.classList.add('navigating');
    
    // Perform the navigation
    window.location.href = url;
}

function getScrollbarWidth() {
    return 0; // Force scrollbar width to always be 0
}

function applyScrollbarPadding() {
    const scrollbarWidth = getScrollbarWidth();
    document.documentElement.style.setProperty('--scrollbar-width', `${scrollbarWidth}px`);
}

// Toggle hamburger menu
function toggleMenu() {
    const hamburgerMenu = document.querySelector('.hamburger-menu');
    const sidebarMenu = document.querySelector('.sidebar-menu');
    const menuOverlay = document.querySelector('.menu-overlay');

    // Toggle classes
    hamburgerMenu.classList.toggle('open');
    sidebarMenu.classList.toggle('open');
    menuOverlay.classList.toggle('open');

    // Toggle menu-open class on body
    document.body.classList.toggle('menu-open');
    
    // Toggle side-menu-active class on html element
    document.documentElement.classList.toggle('side-menu-active');

    // Remove pulsating effect when menu is open
    if (document.body.classList.contains('menu-open')) {
        hamburgerMenu.classList.remove('pulse-attention');
    } else {
        setTimeout(() => {
            if (!hamburgerMenu.classList.contains('open')) {
                hamburgerMenu.classList.add('pulse-attention');
                setTimeout(() => {
                    hamburgerMenu.classList.remove('pulse-attention');
                }, 1500);
            }
        }, 300);
    }
}

// Close menu when clicking on overlay
function closeMenuOnOverlayClick() {
    const menuOverlay = document.querySelector('.menu-overlay');
    if (menuOverlay) {
        menuOverlay.addEventListener('click', toggleMenu);
    }
}

// Set active menu item based on current page - this is now handled server-side with Razor
// but keeping a fallback for any dynamic page changes
function setActiveMenuItem() {
    const currentPath = window.location.pathname;
    const menuItems = document.querySelectorAll('.sidebar-menu a');

    menuItems.forEach(item => {
        const itemPath = item.getAttribute('href');
        
        // Remove any existing active classes
        item.classList.remove('active');
        
        // Add active class if the paths match
        if (currentPath === itemPath || 
            (currentPath === '/' && itemPath === '/Index') ||
            (itemPath !== '/Index' && currentPath.startsWith(itemPath))) {
            item.classList.add('active');
        }
    });
}

// Handle form submission with validation
function setupFormValidation() {
    const forms = document.querySelectorAll('form.needs-validation');

    forms.forEach(form => {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }

            form.classList.add('was-validated');
        });
    });
}

// Handle sidebar navigation
function setupSidebarNavigation() {
    const sidebarLinks = document.querySelectorAll('.sidebar-menu a');
    
    sidebarLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Get the href
            const url = this.getAttribute('href');
            
            // Remove active class from all links
            sidebarLinks.forEach(l => l.classList.remove('active'));
            
            // Add active class to clicked link
            this.classList.add('active');
            
            // Close the menu if it's open
            const hamburgerMenu = document.querySelector('.hamburger-menu');
            if (hamburgerMenu.classList.contains('open')) {
                toggleMenu();
            }
            
            // Navigate to the new page
            navigateTo(url);
        });
    });
}

document.addEventListener('DOMContentLoaded', function() {
    // Calculate scrollbar width and set CSS variable
    applyScrollbarPadding();
    
    // Add resize listener
    window.addEventListener('resize', applyScrollbarPadding);
    
    // Setup hamburger menu first to ensure it's initialized properly
    const hamburgerMenu = document.querySelector('.hamburger-menu');
    if (hamburgerMenu) {
        hamburgerMenu.addEventListener('click', toggleMenu);
        // Add a pulsating effect to draw attention to the menu button on first load
        setTimeout(() => {
            hamburgerMenu.classList.add('pulse-attention');
            setTimeout(() => {
                hamburgerMenu.classList.remove('pulse-attention');
            }, 2000);
        }, 500);
        closeMenuOnOverlayClick();
        setActiveMenuItem();
    }

    // Add table hover class to all tables
    const tables = document.querySelectorAll('table');
    tables.forEach(table => {
        table.classList.add('table-hover');
    });

    // Add confirmation to delete buttons that don't already have it
    const deleteLinks = document.querySelectorAll('a[href*="Delete"]');
    deleteLinks.forEach(link => {
        if (!link.hasAttribute('onclick')) {
            link.setAttribute('onclick', 'return confirm("Are you sure you want to delete this item?")');
        }
    });

    // Add animation to cards
    const cards = document.querySelectorAll('.card');
    cards.forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-5px)';
            this.style.boxShadow = '0 0.5rem 1rem rgba(0, 0, 0, 0.15)';
        });

        card.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0)';
            this.style.boxShadow = '0 0.125rem 0.25rem rgba(0, 0, 0, 0.075)';
        });
    });

    // Add click events to navigation buttons
    const navigationButtons = document.querySelectorAll('.navigation-button, .cfa-btn');
    navigationButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const url = this.getAttribute('href');
            if (url) {
                e.preventDefault();
                navigateTo(url);
            }
        });
    });

    // The hamburger menu has already been initialized at the top of the function

    // Setup form validation
    setupFormValidation();

    // Initialize toggle switches if any
    const toggleSwitches = document.querySelectorAll('.toggle-switch input');
    toggleSwitches.forEach(toggle => {
        toggle.addEventListener('change', function () {
            const targetId = this.getAttribute('data-target');
            if (targetId) {
                const target = document.getElementById(targetId);
                if (target) {
                    if (this.checked) {
                        target.style.display = 'block';
                    } else {
                        target.style.display = 'none';
                    }
                }
            }
        });
        // Trigger change event to initialize state
        toggle.dispatchEvent(new Event('change'));
    });

    // Add the sidebar navigation setup
    setupSidebarNavigation();
});