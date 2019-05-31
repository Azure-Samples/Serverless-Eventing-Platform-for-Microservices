import { Injectable } from '@angular/core';
import { AppInsights } from 'applicationinsights-js';
import { ConfigurationService} from '../../app/configuration/configuration.service';
@Injectable()
export class AppInsightsService {
  
  private config:Microsoft.ApplicationInsights.IConfig={
    instrumentationKey:''
  }
  
  constructor(configuration: ConfigurationService) {
    this.config.instrumentationKey=configuration.appInsightsKey;
    if(!AppInsights.config){
      AppInsights.downloadAndSetup(this.config);
    }
   }
   
   logPageView(name: string,url?: string,properties?: any, measurements?:any, duration?: number){
     AppInsights.trackPageView(name,url,properties,measurements,duration);
   }

   logEvent(name:string,properties?: any, measurements?: any){
     AppInsights.trackEvent(name, properties,measurements)
   }

}
