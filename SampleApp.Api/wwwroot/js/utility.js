
export default class Utility {

    constructor(elSpinner, elMessage, throwOnError) {
        this.elSpinner = elSpinner;
        this.elMessage = elMessage;
        this.throwOnError = throwOnError;
    }

    async HttpSend(method, url, data = null, contentType = "application/json", parseResponseType = "json") {

        let response;
        try {
            if (!contentType) contentType = "application/json";
            const options = {
                method: method, // *GET, POST, PUT, DELETE, etc.
                //mode: 'cors', // no-cors, *cors, same-origin
                //cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
                //credentials: 'same-origin', // include, *same-origin, omit
                headers: {
                    'Content-Type': contentType //outgoing type
                },
                //redirect: 'follow', // manual, *follow, error
                //referrerPolicy: 'no-referrer', // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
                body: (data) ? JSON.stringify(data) : null // body data type must match "Content-Type" header
            };
            this.toggleSpinner(true);
            response = await fetch(url, options);
            if (response.ok) {
                switch (parseResponseType) {
                    case "json":
                        data = await response.json();
                        break;
                    case "text":
                        data = await response.text();
                        break;
                    default:
                        data = null;
                        break;
                }
                return { ok: response.ok, statusCode: response.status, data: data };
            }
            else {
                let err = `Status: ${response.status} `;
                try {
                    //try to parse extra validation info from response
                    const jsonErr = await response.json();
                    err += `${jsonErr.detail ?? jsonErr.message}`;
                }
                catch {
                    //igonore parsing error
                }
                throw new Error(err);
            }
        }
        catch (error) {
            console.error(error);
            this.elMessage.classList.add("error");
            this.elMessage.innerText = error;
            if (this.throwOnError)
                throw error;
            else
                return { statusCode: response.status, data: data };
        }
        finally {
            this.toggleSpinner(false);
        }
    }

    toggleSpinner = (toggle) => {
        if (toggle) this.elMessage.innerText = "";
        if (!this.elSpinner) return;
        toggle ? this.elSpinner.removeAttribute("hidden") : this.elSpinner.setAttribute("hidden", true);
    }

    showSpinner = (elParent, classForSpinner, scrollDown) => {
        const elSpin = document.createElement('div');
        if (classForSpinner) {
            elSpin.classList.add(classForSpinner);
        }
        const spin = document.getElementById('spinner-rings').cloneNode(true);
        spin.removeAttribute('hidden');
        elSpin.appendChild(spin);
        elParent.appendChild(elSpin);
        if (elParent && scrollDown) {
            elParent.scrollTop = elParent.scrollHeight;
        }
        return elSpin;
    }

}