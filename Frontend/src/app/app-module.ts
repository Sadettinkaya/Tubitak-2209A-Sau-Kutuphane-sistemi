import { NgModule, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { BrowserModule, provideClientHydration, withEventReplay } from '@angular/platform-browser';

import { HttpClientModule } from '@angular/common/http'; // DEPRECATED uyarısını verebilir ama çalışır

import { AppRoutingModule } from './app-routing-module';
import { AppComponent  } from './app';
import { ReservationFilterComponent  } from './reservation-filter/reservation-filter.component';
import { HomeComponent } from './home/home.component';
import { TurnstileComponent } from './turnstile/turnstile.component';
import { LoginComponent } from './login/login.component';
import { FormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    AppComponent ,
    ReservationFilterComponent,
    HomeComponent,
    TurnstileComponent,
    LoginComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule ,

  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideClientHydration(withEventReplay())
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
