import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LeaderboardComponent } from './components/leaderboard/leaderboard.component';
import { StatsComponent } from './components/stats/stats.component';
import { TotalsComponent } from './components/totals/totals.component';

const routes: Routes = [
  {
    path: 'leaderboard', component: LeaderboardComponent
  },
  {
    path: 'stats', component: StatsComponent
  },
  {
    path: 'totals', component: TotalsComponent

  },
  {
    path: '',
    redirectTo: 'leaderboard',
    pathMatch: 'full'
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
