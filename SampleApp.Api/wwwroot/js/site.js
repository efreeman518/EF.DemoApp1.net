const uri = 'api/v1/TodoItems';

const TodoItemStatus = {
    None: 0,
    Created: 1,
    Accepted: 2,
    InProgress: 3,
    Completed: 4,
};

let todos = [];

function handleErrors(response) {
    toggleLoader(false);
    if (response.ok) {
        document.getElementById('error').innerText = "";
        return response;
    }
    return response.json().then(err => {
        document.getElementById('error').innerText = err.detail;
        throw err.Message;
    });
}

function getItems() {
    toggleLoader(true);
    fetch(uri)
        .then(response => handleErrors(response))
        .then(response => response.json())
        .then(json => _displayItems(json.data))
        .catch(error => console.error('Unable to get items.', error));
}

function deleteItem(id) {
    toggleLoader(true);
    fetch(`${uri}/${id}`, {
        method: 'DELETE'
    })
        .then(response => handleErrors(response))
        .then(() => getItems())
        .catch(error => console.error('Unable to delete item.', error));
}

function displayEditForm(id) {
    document.getElementById('error').innerText = "";

    const item = todos.find(i => i.id === id);
    document.getElementById('edit-row').setAttribute("data-id", id);
    document.getElementById('btn-save').textContent = 'Update';
    document.getElementById('edit-name').value = item.name;
    document.getElementById('edit-isComplete').checked = item.status == TodoItemStatus.Completed;
    document.getElementById('edit-secure-random').value = item.secureRandom;
    document.getElementById('edit-secure-deterministic').value = item.secureDeterministic;
}

function saveItem() {
    toggleLoader(true);

    const item = {
        status: document.getElementById('edit-isComplete').checked ? TodoItemStatus.Completed : TodoItemStatus.Accepted,
        name: document.getElementById('edit-name').value.trim(),
        secureRandom: document.getElementById('edit-secure-random').value.trim(),
        secureDeterministic: document.getElementById('edit-secure-deterministic').value.trim()
    };
    let method = "POST";
    let url = `${uri}`;
    let itemId = document.getElementById('edit-row').getAttribute("data-id");
    if (itemId != "") {
        item.id = itemId;
        method = "PUT";
        url = url + `/${itemId}`;
    }

    fetch(url, {
        method: method,
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(item)
    })
        .then(response => handleErrors(response))
        .then(() => { clearEditRow(); getItems(); })
        .catch(error => console.error('Unable to update item.', error));

     return false;
}

function clearEditRow() {
    document.getElementById('edit-row').setAttribute("data-id", "");
    document.getElementById('edit-isComplete').checked = false;
    document.getElementById('edit-name').value = "";
    document.getElementById('edit-secure-random').value = "";
    document.getElementById('edit-secure-deterministic').value = "";
    document.getElementById('btn-save').textContent = 'Add';
}

function _displayCount(itemCount) {
    const name = (itemCount === 1) ? 'to-do' : 'to-dos';
    document.getElementById('counter').innerText = `${itemCount} ${name}`;
}

function _displayItems(data) {
    const tBody = document.getElementById('todos');
    tBody.innerHTML = '';

    _displayCount(data.length);

    const button = document.createElement('button');

    data.forEach(item => {
        let isCompleteCheckbox = document.createElement('input');
        isCompleteCheckbox.type = 'checkbox';
        isCompleteCheckbox.disabled = true;
        isCompleteCheckbox.checked = item.status == TodoItemStatus.Completed;

        let editButton = button.cloneNode(false);
        editButton.innerText = 'Edit';
        editButton.setAttribute('onclick', `displayEditForm('${item.id}')`);

        let deleteButton = button.cloneNode(false);
        deleteButton.innerText = 'Delete';
        deleteButton.setAttribute('onclick', `deleteItem('${item.id}')`);

        let tr = tBody.insertRow();

        let td = tr.insertCell(0);
        td.appendChild(isCompleteCheckbox);

        td = tr.insertCell(1);
        let textNode = document.createTextNode(item.name);
        td.appendChild(textNode);

        td = tr.insertCell(2);
        textNode = document.createTextNode(item.status);
        td.appendChild(textNode);

        td = tr.insertCell(3);
        textNode = document.createTextNode(item.secureRandom);
        td.appendChild(textNode);

        td = tr.insertCell(4);
        textNode = document.createTextNode(item.secureDeterministic);
        td.appendChild(textNode);

        td = tr.insertCell(5);
        td.appendChild(editButton);

        td = tr.insertCell(6);
        td.appendChild(deleteButton);
    });

    todos = data;
}

function toggleLoader(toggle) {
    document.querySelector(".loader").style.display = toggle ? "block" : "none";
}