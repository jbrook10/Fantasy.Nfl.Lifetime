import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { ILeagueData, IOwner } from 'src/app/models/models';
import { DataService } from 'src/app/service/data.service';

@Component({
  selector: 'app-leaderboard',
  templateUrl: './leaderboard.component.html',
  styleUrls: ['./leaderboard.component.scss']
})
export class LeaderboardComponent implements OnInit, OnDestroy {

  SmallScreen = false;
  LeagueData!: ILeagueData;
  displayedColumns = ['Name', 'CountingScore', 'Score'];

  dataSource = new MatTableDataSource<IOwner>();

  $Destroyed = new Subject();

  @ViewChild(MatSort) sort: MatSort = new MatSort();


  constructor(private dataService: DataService, breakpointObserver: BreakpointObserver) {
    breakpointObserver.observe([
      Breakpoints.Small,
      Breakpoints.XSmall,
    ]).subscribe(result => {
      this.SmallScreen = result.matches;
    });

  }
  ngOnDestroy(): void {
    this.$Destroyed.next(null);
    this.$Destroyed.complete();
  }

  ngOnInit(): void {
    this.dataService.LeagueData$.pipe(takeUntil(this.$Destroyed)).subscribe(data => {
      this.LeagueData = data;
      this.buildData();

    });

  }

  buildData(): void {

    this.LeagueData.Owners.sort((a, b) => b.CountingScore - a.CountingScore);

    this.dataSource.data = this.LeagueData.Owners;
    this.dataSource.sort = this.sort;

    const sortState: Sort = {active: 'CountingScore', direction: 'desc'};
    this.sort.active = sortState.active;
    this.sort.direction = sortState.direction;
    this.sort.sortChange.emit(sortState);
  }

}
