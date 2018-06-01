import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { HubConnectionBuilder } from '@aspnet/signalr'

@Injectable()
export class HubService {
    private hubConnection;

    constructor(){ }

    public createConnection(location: string, userid: string){
        let qs = "?userId=" + userid
        this.hubConnection = new HubConnectionBuilder().withUrl(location + "/event" + qs).build();
    }
    public getHubConnection(){
        return this.hubConnection
    }
    
    public setHubConnection(h_connect : HubConnectionBuilder){
        this.hubConnection = h_connect
    }
}