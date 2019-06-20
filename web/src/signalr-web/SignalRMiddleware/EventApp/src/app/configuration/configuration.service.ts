import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable()
export class ConfigurationService {

    private configuration: IAppInsightsConfiguration;
    
    constructor(private http: HttpClient) { }

    loadConfig() {
        var appsetting_get_url = location.origin + "/api/applicationsetting";
        return this.http.get<IAppInsightsConfiguration>(appsetting_get_url)
            .toPromise()
            .then(result => {
                this.configuration = <IAppInsightsConfiguration>(result);
            }, error => console.error(error));
    }

    get appInsightsKey() {
        return this.configuration.key;
    }


}
export interface IAppInsightsConfiguration {
    key: string;
}