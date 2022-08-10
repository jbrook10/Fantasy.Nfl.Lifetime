import { Component, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { ILeagueData, IPlayer, ITotalView } from 'src/app/models/models';
import { DataService } from 'src/app/service/data.service';
import { takeUntil } from 'rxjs/operators';
import { SelectionModel } from '@angular/cdk/collections';


@Component({
  selector: 'app-totals',
  templateUrl: './totals.component.html',
  styleUrls: ['./totals.component.scss']
})
export class TotalsComponent implements OnInit {

  LeagueData!: ILeagueData;
  viewType = 'All';
  totalType = 0;
  players: IPlayer[] = [];
  filteredPlayers: IPlayer[] = [];
  ownerTotals: ITotalView[] = [];
  selection = new SelectionModel<ITotalView>(false, []);

  columns = ['Rank', 'Owner', 'Name', 'Position', 'Points'];
  ownerTotalColumns = ['Owner', 'Count', 'Average'];


  $Destroyed = new Subject();

  constructor(private dataService: DataService) { }

  ngOnInit(): void {
    this.dataService.LeagueData$.pipe(takeUntil(this.$Destroyed)).subscribe(data => {
      this.LeagueData = data;
      this.players = this.LeagueData.Owners.flatMap(o => o.Players);
      this.players.sort((a, b) => b.Score - a.Score);
      this.totalType = this.players.length;

      this.FilterList()
    });
  }

  FilterList(): void {
    this.filteredPlayers = this.players.slice(0, +this.totalType);

    // get the
    this.ownerTotals = [];
    this.filteredPlayers.map((p, index) => {

      let currentOwner = this.ownerTotals.find(o => o.FantasyOwner === p.FantasyOwner);
      if (!!currentOwner) {
        currentOwner.Count = currentOwner.Count + 1;
        currentOwner.IndexTotal = currentOwner.IndexTotal + (index + 1);
      } else {
        currentOwner = {
          FantasyOwner: p.FantasyOwner,
          Count: 1,
          IndexTotal: index + 1
        };
        this.ownerTotals.push(currentOwner);
      }
    });

    this.ownerTotals.sort((a, b) => b.Count - a.Count);
  }
}
