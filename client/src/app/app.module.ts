import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule } from '@angular/forms';
import { SharedModule } from './_modules/shared.module';
import { NavComponent } from './nav/nav.component';
import { HomeComponent } from './home/home.component';
import { RegisterComponent } from './register/register.component';
import { TrafficComponent } from './traffic/traffic.component';
import { LightingComponent } from './lighting/lighting.component';
import { PayComponent } from './pay/pay.component';
import { TrashComponent } from './trash/trash.component';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { MemberComponent } from './member/member.component'; // Import MemberComponent
import { ErrorInterceptor } from './_interceptors/error.interceptor';
import { JwtInterceptor } from './_interceptors/jwt.interceptor';
import { AdminDashboardComponent } from './admin/admin-dashboard/admin-dashboard.component';
import { EventCreateComponent } from './admin/event-create/event-create.component';
import { UserManagementComponent } from './admin/user-management/user-management.component';

@NgModule({
  declarations: [
    AppComponent,
    NavComponent,
    HomeComponent,
    RegisterComponent,
    TrafficComponent,
    LightingComponent,
    PayComponent,
    TrashComponent,
    TestErrorComponent,
    NotFoundComponent,
    ServerErrorComponent,
    MemberComponent,
    AdminDashboardComponent,
    EventCreateComponent,
    UserManagementComponent 
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    BrowserAnimationsModule,
    FormsModule,
    SharedModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
