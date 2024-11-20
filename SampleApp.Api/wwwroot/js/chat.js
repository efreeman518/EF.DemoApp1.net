import Utility from './utility.js';

const _utility = new Utility(document.querySelector(".spinner"), document.querySelector("#message"), true);
const urlChat = 'api1/v1.1/chat';

let chatId = null;

async function sendMessage() {
    

    //send 
    const method = "POST";
    const url = urlChat;
    const payload = { "ChatId": chatId, "Message": document.getElementById('chat-input').value.trim() };
    
    const response = await _utility.HttpSend(method, url, payload);
    if (response.ok) {

        //get response
        const data = await response.data;

        //hold the chat Id
        chatId = data.chatId;


        //append user message to the chat output
        appendMessage('user', document.getElementById('chat-input').value);
        appendMessage('system', data.message);

        //clear input
        document.getElementById('chat-input').value = "";
    }
}

function appendMessage(source, message) {
    let msg = document.createElement('div');
    msg.classList.add("message", "message-" +source);
    msg.innerHTML = message;
    document.getElementById('chat-output').appendChild(msg);
}

function newChat() {
    chatId = null;
}

//wire up event handlers
document.getElementById('chat-send').addEventListener('click', sendMessage);
document.getElementById('chat-new').addEventListener('click', newChat);
document.addEventListener('keydown', function (event) {
    if (event.key === 'Enter') {
        sendMessage();
    }
});
