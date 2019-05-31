import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable()
export class ConfigurationService {

    private configuration: IAppInsightsConfiguration;
    constructor(private http: HttpClient) { }

    loadConfig() {
        return this.http.get<IAppInsightsConfiguration>('/api/applicationsetting')
            .toPromise()
            .then(result => {
                this.configuration = <IAppInsightsConfiguration>(result);
            }, error => console.error(error));
    }

    get apiAddress() {
        return this.configuration.key;
    }


}
export interface IAppInsightsConfiguration {
    key: string;
}