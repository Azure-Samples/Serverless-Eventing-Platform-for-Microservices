import { Component } from '@angular/core'
import { Location } from '@angular/common'
import { HubConnectionBuilder } from '@aspnet/signalr'

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent {
  title = 'app';
  private hubConnection;

  name = '';
  message = '';
  messages: string[] = [];
  eventData = '';
  userid = '';
  location: Location

  ngOnInit(){
  }

  
  public connectToHub(): void {
    let qs = "?userId=" + this.userid
    this.hubConnection = new HubConnectionBuilder().withUrl(location.origin + "/event" + qs).build();
    
    this.hubConnection.on('onEvent',(message: string) => {
      this.eventData = message
    });
    // Connect to Hub
    this.hubConnection.start().then(() => {
      console.log("Connection started successfully")
    
      this.hubConnection
        .invoke('Login', this.userid)
    .   catch(err => console.error(err))
     } )
    .catch(err => console.log('Error while establishing connection'));
   
  }
}
