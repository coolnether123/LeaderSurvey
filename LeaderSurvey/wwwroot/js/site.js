// Site JavaScript
function navigateTo(url) {
    document.body.classList.add('navigating');
    window.location.href = url;
}

function getScrollbarWidth() {
    return 0; // Force scrollbar width to always be 0
}

function applyScrollbarPadding() {
    const scrollbarWidth = getScrollbarWidth();
    document.documentElement.style.setProperty('--scrollbar-width', `${scrollbarWidth}px`);
}

function saveMenuState(isOpen) {
    localStorage.setItem('menuState', isOpen ? 'open' : 'closed');
}

function getMenuState() {
    return localStorage.getItem('menuState') === 'open';
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
    
    // Save the menu state
    saveMenuState(hamburgerMenu.classList.contains('open'));
    
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
    // Add click events to navigation buttons
    const navigationButtons = document.querySelectorAll('.navigation-button, .cfa-btn, .nav-item');
    navigationButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const url = this.getAttribute('href');
            if (url) {
                e.preventDefault();
                navigateTo(url);
            }
        });
    });

    // Add table hover class to all tables
    const tables = document.querySelectorAll('table');
    tables.forEach(table => {
        table.classList.add('table-hover');
    });

    // Add confirmation to delete buttons
    const deleteLinks = document.querySelectorAll('a[href*="Delete"]');
    deleteLinks.forEach(link => {
        if (!link.hasAttribute('onclick')) {
            link.setAttribute('onclick', 'return confirm("Are you sure you want to delete this item?")');
        }
    });

    // Setup form validation
    setupFormValidation();
});

// Question handling
let questionCount = 0;

function addQuestion() {
    if (questionCount >= 10) {
        alert("Maximum of 10 questions reached!");
        return;
    }

    questionCount++;
    updateQuestionCounter();

    const questionItem = document.createElement('div');
    questionItem.className = 'question-item d-flex align-items-center';
    questionItem.innerHTML = `
        <i class="bi bi-grip-vertical question-drag-handle"></i>
        <div class="question-number">${questionCount}</div>
        <div class="flex-grow-1">
            <input type="text" class="form-control mb-2" placeholder="Enter your question here..." required>
            <select class="question-type-select">
                <option value="yesno">Yes/No</option>
                <option value="score">Score (0-10)</option>
            </select>
        </div>
        <button class="cfa-btn cfa-btn-sm cfa-btn-outline ms-3" onclick="deleteQuestion(this)">
            <i class="bi bi-trash"></i>
        </button>
        <div class="my-pleasure">My Pleasure!</div>
    `;

    document.getElementById('questions-container').appendChild(questionItem);
    initializeSortable();
}

function deleteQuestion(button) {
    if (confirm('Are you sure you would like to delete this question?')) {
        const questionItem = button.closest('.question-item');
        const myPleasure = questionItem.querySelector('.my-pleasure');
        
        // Show "My Pleasure!" message
        myPleasure.style.opacity = '1';
        
        // Add deleting animation class
        questionItem.classList.add('deleting');
        
        // Remove after animation
        setTimeout(() => {
            questionItem.remove();
            questionCount--;
            updateQuestionCounter();
            renumberQuestions();
        }, 500);
    }
}

function updateQuestionCounter() {
    document.getElementById('question-counter').textContent = `${questionCount}/10`;
}

function renumberQuestions() {
    const questions = document.querySelectorAll('.question-number');
    questions.forEach((question, index) => {
        question.textContent = index + 1;
    });
}

function initializeSortable() {
    new Sortable(document.getElementById('questions-container'), {
        animation: 150,
        handle: '.question-drag-handle',
        ghostClass: 'sortable-ghost',
        onEnd: renumberQuestions
    });
}

// Input validation for score type
document.addEventListener('input', function(e) {
    if (e.target.matches('.number-input')) {
        let value = parseInt(e.target.value);
        if (isNaN(value)) {
            e.target.value = '';
        } else {
            value = Math.min(10, Math.max(0, value));
            e.target.value = value;
        }
    }
});