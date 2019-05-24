import { app, BrowserWindow, dialog } from 'electron';
import { CommunicationClient, Communicator } from './comm';

class Main {
    private handle_request(req) {
        console.log('Handling request', req);
        return null;
    }

    public Run(): void {
        process.on('uncaughtException', (err: Error) => {
            const messageBoxOptions = {
                type: 'error',
                title: 'Error in Client process',
                message: err.toString()
            };
            dialog.showMessageBox(messageBoxOptions);
            app.quit();
        });

        console.log('Starting');
        this.client = new CommunicationClient(process.env);
        this.ui_communicator = this.client.StartWindowControl(this.handle_request.bind(this));
        var win = new BrowserWindow();
        win.on("close", (event) => app.quit());
        win.on("minimize", (event) => {
            console.log('sending');
            this.ui_communicator.SendMessage({ window: "main", action: Date.now().toString() }, (resp) => dialog.showMessageBox(resp));
        });
        win.show();
      //  setInterval(() => this.ui_communicator.SendMessage({ window: "main", action: Date.now().toString() }, (resp) => dialog.showMessageBox(resp)), 1000);
    }

    private client: CommunicationClient;
    private ui_communicator: Communicator;
}

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on('ready', () => new Main().Run());

// Quit when all windows are closed.
app.on('window-all-closed', () => {
    app.quit();
})
