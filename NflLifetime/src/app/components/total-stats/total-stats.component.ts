import { IPlayer, PositionType } from './../../models/models';
import { Component, Input, OnInit } from '@angular/core';
import { IOwner } from 'src/app/models/models';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-total-stats',
  templateUrl: './total-stats.component.html',
  styleUrls: ['./total-stats.component.scss']
})
export class TotalStatsComponent implements OnInit {

  countingPlayers: IPlayer[] = [];
  displayedColumns = ['Name', 'Position', 'Score'];
  dataSource = new MatTableDataSource<IPlayer>();


  @Input() owner!: IOwner;
  @Input() onlyCounting!: boolean;
  @Input() smallScreen!: boolean;

  constructor() { }

  ngOnInit(): void {
    this.buildPlayers();
    this.dataSource.data = this.countingPlayers;
  }

  private buildPlayers() {
      this.AddPlayers('qb', 1, !this.onlyCounting);
      this.AddPlayers('rb', 2, !this.onlyCounting);
      this.AddPlayers('wr', 3, !this.onlyCounting);
      this.AddPlayers('te', 1, !this.onlyCounting);
  }

  private AddPlayers(position: string, num: number, showAll: boolean) : void {
    const matchingPlayers = this.owner.Players.filter(p => p.Position == position);
    matchingPlayers.sort((a, b) => b.Score - a.Score);
    const topN = matchingPlayers.slice(0, num);
    topN.forEach(p => p.Scoring = true && showAll);

    if (showAll) {
      topN.push(...matchingPlayers.slice(num));
    }

    this.countingPlayers.push(...topN);
  }

}


