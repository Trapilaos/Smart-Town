import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { PayComponent } from './pay/pay.component';
import { TrafficComponent } from './traffic/traffic.component';
import { LightingComponent } from './lighting/lighting.component';
import { authGuard } from './_guards/auth.guard';
import { adminGuard } from './_guards/admin.guard';
import { TestErrorComponent } from './errors/test-error/test-error.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrorComponent } from './errors/server-error/server-error.component';
import { MemberComponent } from './member/member.component';
import { AdminDashboardComponent } from './admin/admin-dashboard/admin-dashboard.component';
import { UserManagementComponent } from './admin/user-management/user-management.component';
import { EventsListComponent } from './events/list/events-list.component';
import { EventDetailComponent } from './events/detail/event-detail.component';
import { ParkingMapComponent } from './parking/parking-map.component';
import { WasteMapComponent } from './waste/waste-map/waste-map.component';
import { MyInvoicesComponent } from './my-invoices/my-invoices.component';
import { AdminMunicipalityComponent } from './admin/municipality/admin-municipality.component';
import { AdminBinsComponent } from './admin/bins/admin-bins.component';
import { AdminRouteComponent } from './admin/route/admin-route.component';
import { AdminParkingComponent } from './admin/parking/admin-parking.component';
import { AdminEventsComponent } from './admin/events/admin-events.component';
import { AdminEventFormComponent } from './admin/event-form/admin-event-form.component';
import { AdminInvoicesComponent } from './admin/invoices/admin-invoices.component';
import { AdminInvoiceFormComponent } from './admin/invoice-form/admin-invoice-form.component';
import { AdminAuditComponent } from './admin/audit/admin-audit.component';
import { environment } from '../environments/environment';

const routes: Routes = [
  { path: '', component: HomeComponent },

  // Public pages: no guard.
  { path: 'events', component: EventsListComponent },
  { path: 'events/:id', component: EventDetailComponent },
  { path: 'parking', component: ParkingMapComponent },
  { path: 'waste', component: WasteMapComponent },
  { path: 'lighting', component: LightingComponent },
  { path: 'pay', component: PayComponent },

  // Member pages.
  { path: 'member', component: MemberComponent, canActivate: [authGuard] },
  { path: 'my/invoices', component: MyInvoicesComponent, canActivate: [authGuard] },
  { path: 'traffic', component: TrafficComponent, canActivate: [authGuard] },

  // Admin pages.
  { path: 'admin', component: AdminDashboardComponent, canActivate: [adminGuard] },
  { path: 'admin/municipality', component: AdminMunicipalityComponent, canActivate: [adminGuard] },
  { path: 'admin/bins', component: AdminBinsComponent, canActivate: [adminGuard] },
  { path: 'admin/route', component: AdminRouteComponent, canActivate: [adminGuard] },
  { path: 'admin/parking', component: AdminParkingComponent, canActivate: [adminGuard] },
  { path: 'admin/events', component: AdminEventsComponent, canActivate: [adminGuard] },
  { path: 'admin/events/new', component: AdminEventFormComponent, canActivate: [adminGuard] },
  { path: 'admin/events/:id/edit', component: AdminEventFormComponent, canActivate: [adminGuard] },
  { path: 'admin/invoices', component: AdminInvoicesComponent, canActivate: [adminGuard] },
  { path: 'admin/invoices/new', component: AdminInvoiceFormComponent, canActivate: [adminGuard] },
  { path: 'admin/users', component: UserManagementComponent, canActivate: [adminGuard] },
  { path: 'admin/audit', component: AdminAuditComponent, canActivate: [adminGuard] },

  // Old routes kept as redirects so existing links keep working.
  { path: 'trash', redirectTo: 'waste', pathMatch: 'full' },
  { path: 'admin/create-event', redirectTo: 'admin/events/new', pathMatch: 'full' },
  { path: 'admin/user-management', redirectTo: 'admin/users', pathMatch: 'full' },

  // The debug error page is only available outside production builds.
  ...(environment.production ? [] : [{ path: 'errors', component: TestErrorComponent }]),
  { path: 'not-found', component: NotFoundComponent },
  { path: 'server-error', component: ServerErrorComponent },
  { path: '**', component: NotFoundComponent, pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
