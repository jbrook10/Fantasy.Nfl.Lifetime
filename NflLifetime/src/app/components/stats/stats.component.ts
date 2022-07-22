import { takeUntil } from 'rxjs/operators';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, OnInit } from '@angular/core';
import { ILeagueData } from 'src/app/models/models';
import { DataService } from 'src/app/service/data.service';
import { Sort } from '@angular/material/sort';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-stats',
  templateUrl: './stats.component.html',
  styleUrls: ['./stats.component.scss']
})
export class StatsComponent implements OnInit {

  LeagueData!: ILeagueData;

  $Destroyed = new Subject();

  constructor(private dataService: DataService, breakpointObserver: BreakpointObserver) {
    breakpointObserver.observe([
      Breakpoints.Small,
      Breakpoints.XSmall,
    ]).subscribe(result => {
      // this.SmallScreen = result.matches;
    });
   }

  ngOnInit(): void {
    this.dataService.LeagueData$.pipe(takeUntil(this.$Destroyed)).subscribe(data => {
      this.LeagueData = data;
    });
  }

}
