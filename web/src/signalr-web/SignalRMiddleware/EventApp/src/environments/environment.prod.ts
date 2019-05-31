import {ConfigurationService} from '../app/configuration/configuration.service'

export const environment = {
   production: true,
   appInsights: { 
    instrumentationKey: ""/*The web middleware should return the key to the Angular Environment*/
  }
};
