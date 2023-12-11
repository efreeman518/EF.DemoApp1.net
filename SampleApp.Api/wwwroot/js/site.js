import Utility from './utility.js';

const _utility = new Utility(document.querySelector(".spinner"), document.querySelector("#message"));
const urlTodo = 'api/v1/TodoItems';

const TodoItemStatus = {
    None: 0,
    Created: 1,
    Accepted: 2,
    InProgress: 3,
    Completed: 4,
};

let todos = [];

async function getItems() {
    const response = await _utility.HttpSend("GET", urlTodo);
    displayItems(response.data);
}

function editItem(item) {
    popEdit(item.id, item.status == TodoItemStatus.Completed, item.name, item.secureRandom, item.secureDeterministic);
    document.getElementById('btn-save').textContent = 'Update';
}

async function deleteItem(id) {
    clearEditRow();
    const url = `${urlTodo}/${id}`; 
    await _utility.HttpSend("DELETE", url, null, null, "");
    await getItems();
}

async function saveItem() {
    if (document.getElementById('edit-name').value.trim().length == 0) throw new Error("Name required.");

    const item = {
        status: document.getElementById('edit-isComplete').checked ? TodoItemStatus.Completed : TodoItemStatus.Accepted,
        name: document.getElementById('edit-name').value.trim(),
        secureRandom: document.getElementById('edit-secure-random').value.trim(),
        secureDeterministic: document.getElementById('edit-secure-deterministic').value.trim()
    };

    //save - new insert (POST) or update (PUT)
    let method = "POST";
    let url = urlTodo;
    let itemId = document.getElementById('edit-row').getAttribute("data-id");
    if (itemId != "") {
        item.id = itemId;
        method = "PUT";
        url = `${url}/${itemId}`; 
    }

    const response = await _utility.HttpSend(method, url, item);
    if (response.ok) {
        clearEditRow();
        await getItems();
    }
}

function clearEditRow() {
    popEdit("", false, "", "", "")
    document.getElementById('btn-save').textContent = 'Add';
}

function popEdit(id, isComplete, name, secureRandom, secureDeterministic) {
    document.getElementById('message').innerText = "";
    document.getElementById('edit-row').setAttribute("data-id", id);
    document.getElementById('edit-isComplete').checked = isComplete;
    document.getElementById('edit-name').value = name;
    document.getElementById('edit-secure-random').value = secureRandom;
    document.getElementById('edit-secure-deterministic').value = secureDeterministic;
}

function displayCount(itemCount) {
    const name = (itemCount === 1) ? 'to-do' : 'to-dos';
    document.getElementById('counter').innerText = `${itemCount} ${name}`;
}

function displayItems(data) {
    const tBody = document.getElementById('todos');
    tBody.innerHTML = '';

    const items = data.data;
    displayCount(items.length);

    const button = document.createElement('button');

    items.forEach(item => {
        let isCompleteCheckbox = document.createElement('input');
        isCompleteCheckbox.type = 'checkbox';
        isCompleteCheckbox.disabled = true;
        isCompleteCheckbox.checked = item.status == TodoItemStatus.Completed;

        let editButton = button.cloneNode(false);
        editButton.innerText = 'Edit';
        editButton.addEventListener("click", (event) => { editItem(item); });

        let deleteButton = button.cloneNode(false);
        deleteButton.innerText = 'Delete';
        deleteButton.addEventListener("click", (event) => { deleteItem(item.id); });

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

//wire up event handlers
document.getElementById("btn-save").addEventListener("click", (event) => {
    saveItem();
});

//initialize
getItems();