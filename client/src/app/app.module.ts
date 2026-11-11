import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi, withXhr } from '@angular/common/http';
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
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { MemberComponent } from './member/member.component';
import { ErrorInterceptor } from './_interceptors/error.interceptor';
import { JwtInterceptor } from './_interceptors/jwt.interceptor';
import { AdminDashboardComponent } from './admin/admin-dashboard/admin-dashboard.component';
import { UserManagementComponent } from './admin/user-management/user-management.component';
import { MapComponent } from './_shared/map/map.component';
import { EventsListComponent } from './events/list/events-list.component';
import { EventDetailComponent } from './events/detail/event-detail.component';
import { ParkingMapComponent } from './parking/parking-map.component';
import { WasteMapComponent } from './waste/waste-map/waste-map.component';
import { MyInvoicesComponent } from './my-invoices/my-invoices.component';
import { AdminMunicipalityComponent } from './admin/municipality/admin-municipality.component';
import { AdminBinsComponent } from './admin/bins/admin-bins.component';
import { AdminRouteComponent } from './admin/route/admin-route.component';
import { AdminEventsComponent } from './admin/events/admin-events.component';
import { AdminEventFormComponent } from './admin/event-form/admin-event-form.component';
import { AdminParkingComponent } from './admin/parking/admin-parking.component';
import { AdminInvoicesComponent } from './admin/invoices/admin-invoices.component';
import { AdminInvoiceFormComponent } from './admin/invoice-form/admin-invoice-form.component';
import { AdminAuditComponent } from './admin/audit/admin-audit.component';

@NgModule({ declarations: [
        AppComponent,
        NavComponent,
        HomeComponent,
        RegisterComponent,
        TrafficComponent,
        LightingComponent,
        PayComponent,
        TestErrorComponent,
        NotFoundComponent,
        ServerErrorComponent,
        MemberComponent,
        AdminDashboardComponent,
        UserManagementComponent,
        MapComponent,
        EventsListComponent,
        EventDetailComponent,
        ParkingMapComponent,
        WasteMapComponent,
        MyInvoicesComponent,
        AdminMunicipalityComponent,
        AdminBinsComponent,
        AdminRouteComponent,
        AdminEventsComponent,
        AdminEventFormComponent,
        AdminParkingComponent,
        AdminInvoicesComponent,
        AdminInvoiceFormComponent,
        AdminAuditComponent
    ],
    bootstrap: [AppComponent], imports: [BrowserModule,
        AppRoutingModule,
        BrowserAnimationsModule,
        FormsModule,
        SharedModule], providers: [
        // JwtInterceptor must be registered first: it runs outside ErrorInterceptor, so the
        // error handler sees the Authorization header on requests that carried one.
        { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
        { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
        provideHttpClient(withXhr(), withInterceptorsFromDi())
    ] })
export class AppModule { }
