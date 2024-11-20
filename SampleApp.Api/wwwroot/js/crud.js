import Utility from './utility.js';

const _utility = new Utility(document.querySelector(".spinner"), document.querySelector("#message"), true);
const urlTodo = 'api1/v1.1/TodoItems';

const TodoItemStatus = {
    None: 0,
    Created: 1,
    Accepted: 2,
    InProgress: 3,
    Completed: 4,
};

let pageIndex = 1;
let pageSize = 10;
let itemTotal = 0; //support for paging btnNext

async function getPage() {
    if(pageIndex < 1) pageIndex = 1;
    const url = `${urlTodo}?pageSize=${pageSize}&pageIndex=${pageIndex}`;
    const response = await _utility.HttpSend("GET", url);
    if (response.data != null) {
        itemTotal = response.data.total;
        displayCount();
        displayItems(response.data);
    }
}

function editItem(item) {
    popEdit(item.id, item.status == TodoItemStatus.Completed, item.name, item.secureRandom, item.secureDeterministic);
    document.getElementById('btn-save').textContent = 'Update';
}

async function deleteItem(id) {
    clearEditRow();
    const url = `${urlTodo}/${id}`; 
    await _utility.HttpSend("DELETE", url, null, null, "");
    await getPage();
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
        await getPage();
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

function displayCount() {
    const name = (itemTotal === 1) ? 'to-do' : 'to-dos';
    document.getElementById('counter').innerText = `${itemTotal} ${name}`;
}

function displayItems(data) {
    const tBody = document.getElementById('todos');
    tBody.innerHTML = '';

    const items = data.data;
    displayCount(data.total);

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

    const totalPages = Math.floor(data.total / pageSize) + (data.total % pageSize > 0 ? 1 : 0);  
    currentPageSpan.textContent = `Page ${pageIndex} of ${totalPages}`;
    prevBtn.disabled = currentPage === 1;
    nextBtn.disabled = currentPage === totalPages;
}

const prevBtn = document.getElementById('prevBtn');
const nextBtn = document.getElementById('nextBtn');
const currentPageSpan = document.getElementById('currentPage');
const pageSizeSelect = document.getElementById('pageSizeSelect');

//wire up event handlers
document.getElementById("btn-save").addEventListener("click", (event) => {
    saveItem();
});

prevBtn.addEventListener('click', () => {
    if (pageIndex > 1) {
        pageIndex--;
        getPage();
    }
});

nextBtn.addEventListener('click', () => {
    const maxPage = Math.ceil(itemTotal / pageSize); //need page state ItemTotal for paging
    if (pageIndex < maxPage) {
        pageIndex++;
        getPage();
    }
});

pageSizeSelect.addEventListener('change', (event) => {
    pageSize = parseInt(event.target.value);
    pageIndex = 1; // Reset to first page when changing page size
    getPage();
});

//initialize
getPage();
