import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule }   from '@angular/forms';
import { HttpClientModule } from '@angular/common/http'
import { BrowserAnimationsModule } from '@angular/platform-browser/animations'
import { AppComponent } from './app.component';
import { LoginComponent } from './components/login/login.component';
import { CategoryComponent } from './components/categories/category.component';
import { ItemComponent } from './components/items/item.component';
import { Routes,RouterModule } from '@angular/router';
import { DataService } from './services/data.service';
import { HubService } from './services/hub.service'
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { AppInsightsService } from './services/app-insights.service';
import { APP_INITIALIZER } from '@angular/core';
import { ConfigurationService } from "./configuration/configuration.service";

const appRoutes: Routes = [
  {
    path: '', component: LoginComponent
  },
  {
    path: 'home', component: CategoryComponent
  },
  {
    path: 'items', component: ItemComponent
  }
]

const appInitializerFn = (appConfig: ConfigurationService) => {
  return () => {
    return appConfig.loadConfig();
  };
};

@NgModule({
  
  declarations: [
    AppComponent, LoginComponent, CategoryComponent, ItemComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    BrowserAnimationsModule,
    HttpClientModule,
    NgbModule.forRoot(),
    RouterModule.forRoot(appRoutes, {enableTracing: true})
  ],
  providers: [DataService, HubService, AppInsightsService,ConfigurationService,
    {
      provide: APP_INITIALIZER,
      useFactory: appInitializerFn,
      multi: true,
      deps: [ConfigurationService]
    }],
  bootstrap: [AppComponent]
  
})
export class AppModule { }
