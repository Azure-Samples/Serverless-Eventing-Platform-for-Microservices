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
  providers: [DataService, HubService, AppInsightsService],
  bootstrap: [AppComponent]
})
export class AppModule { }
