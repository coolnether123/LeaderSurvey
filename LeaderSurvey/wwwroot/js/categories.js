// Global variables
let categoryModal;
let deleteCategoryModal;
let isEditing = false;

// Initialize when document is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize Bootstrap modals
    categoryModal = new bootstrap.Modal(document.getElementById('categoryModal'));
    deleteCategoryModal = new bootstrap.Modal(document.getElementById('deleteCategoryModal'));

    // Set up event listeners
    document.getElementById('confirmDeleteBtn').addEventListener('click', confirmDeleteCategory);
});

// Show the category modal for creating a new category
function showCategoryModal() {
    isEditing = false;
    document.getElementById('categoryModalLabel').textContent = 'Create New Category';
    document.getElementById('categoryForm').reset();
    document.getElementById('categoryId').value = '0';
    categoryModal.show();
}

// Show the category modal for editing an existing category
function editCategory(id, name, description) {
    isEditing = true;
    document.getElementById('categoryModalLabel').textContent = 'Edit Category';
    document.getElementById('categoryId').value = id;
    document.getElementById('categoryName').value = name;
    document.getElementById('categoryDescription').value = description;
    categoryModal.show();
}

// Show the delete confirmation modal
function deleteCategory(id, name) {
    document.getElementById('deleteCategoryName').textContent = name;
    document.getElementById('confirmDeleteBtn').dataset.categoryId = id;
    deleteCategoryModal.show();
}

// Save a new or updated category
async function saveCategory() {
    const id = parseInt(document.getElementById('categoryId').value);
    const name = document.getElementById('categoryName').value.trim();
    const description = document.getElementById('categoryDescription').value.trim();

    if (!name) {
        showNotification('Category name is required', 'error');
        return;
    }

    const category = {
        id: id,
        name: name,
        description: description
    };

    try {
        let response;
        if (isEditing) {
            // Update existing category
            response = await fetch(`/api/Categories/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(category)
            });
        } else {
            // Create new category
            response = await fetch('/api/Categories', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(category)
            });
        }

        if (response.ok) {
            showNotification(`Category ${isEditing ? 'updated' : 'created'} successfully`, 'success');
            categoryModal.hide();
            // Reload the page to show the updated categories
            window.location.reload();
        } else {
            const errorData = await response.json();
            showNotification(errorData.message || `Failed to ${isEditing ? 'update' : 'create'} category`, 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification(`An error occurred while ${isEditing ? 'updating' : 'creating'} the category`, 'error');
    }
}

// Confirm and delete a category
async function confirmDeleteCategory() {
    const categoryId = document.getElementById('confirmDeleteBtn').dataset.categoryId;

    try {
        const response = await fetch(`/api/Categories/${categoryId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        });

        if (response.ok) {
            showNotification('Category deleted successfully', 'success');
            deleteCategoryModal.hide();
            // Reload the page to update the categories list
            window.location.reload();
        } else {
            let errorMessage = 'Failed to delete category';
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorMessage;
            } catch (e) {
                // If response is not JSON, use default message
            }
            showNotification(errorMessage, 'error');
            deleteCategoryModal.hide();
        }
    } catch (error) {
        console.error('Error:', error);
        showNotification('An error occurred while deleting the category', 'error');
        deleteCategoryModal.hide();
    }
}

// Show a notification
function showNotification(message, type = 'info') {
    // Create notification element if it doesn't exist
    let notificationContainer = document.getElementById('notification-container');
    if (!notificationContainer) {
        notificationContainer = document.createElement('div');
        notificationContainer.id = 'notification-container';
        notificationContainer.style.position = 'fixed';
        notificationContainer.style.top = '20px';
        notificationContainer.style.right = '20px';
        notificationContainer.style.zIndex = '9999';
        document.body.appendChild(notificationContainer);
    }

    // Create the notification
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible fade show`;
    notification.role = 'alert';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    // Add to container
    notificationContainer.appendChild(notification);

    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            notification.remove();
        }, 300);
    }, 5000);
}
