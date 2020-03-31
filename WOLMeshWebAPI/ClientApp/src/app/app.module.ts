import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { ClarityModule } from '@clr/angular';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { DevicetableComponent } from './shared/devicetable/devicetable.component';
import { BooltoonlinePipe } from './shared/booltoonline.pipe';
import { BooltoresultPipe } from './shared/booltoresult.pipe';
import { NetworktableComponent } from './shared/networktable/networktable.component';
import { SettingsComponent } from './shared/settings/settings.component';
import { Ng5SliderModule } from 'ng5-slider';
import { RecenteventsComponent } from './shared/recentevents/recentevents.component';
import { ManualdevicetableComponent } from './shared/manualdevicetable/manualdevicetable.component';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    DevicetableComponent,
    BooltoonlinePipe,
    BooltoresultPipe,
    NetworktableComponent,
    SettingsComponent,
    RecenteventsComponent,
    ManualdevicetableComponent,
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    Ng5SliderModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
    ]),
    ClarityModule,
    BrowserAnimationsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
