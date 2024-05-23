import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { MemberListComponent } from './members/member-list/member-list.component';
import { MemberDetailComponent } from './members/member-detail/member-detail.component';
import { ListsComponent } from './lists/lists.component';
import { MessagesComponent } from './messages/messages.component';
import { PayComponent } from './pay/pay.component';
import { TrafficComponent } from './traffic/traffic.component';
import { TrashComponent } from './trash/trash.component';
import { LightingComponent } from './lighting/lighting.component';
import { authGuard } from './_guards/auth.guard';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path:'',
    runGuardsAndResolvers: 'always',
    canActivate: [authGuard],
    children: [
      { path: 'members', component: MemberListComponent },
      { path: 'members/:id', component: MemberDetailComponent }, // Note the parameter `:id`
      { path: 'lists', component: ListsComponent },
      { path: 'messages', component: MessagesComponent },
      { path: 'pay', component: PayComponent },
      { path: 'traffic', component: TrafficComponent },
      { path: 'trash', component: TrashComponent },
      { path: 'lighting', component: LightingComponent }
    ]
  },
  { path: '**', component: HomeComponent, pathMatch: 'full'}, 
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
