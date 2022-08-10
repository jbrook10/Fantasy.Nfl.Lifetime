import { Component, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { ILeagueData } from 'src/app/models/models';
import { DataService } from 'src/app/service/data.service';
import { takeUntil } from 'rxjs/operators';
import { DataItem, LegendPosition, MultiSeries, Series } from '@swimlane/ngx-charts';
import { PlayerService } from 'src/app/service/player.service';


@Component({
  selector: 'app-analytics',
  templateUrl: './analytics.component.html',
  styleUrls: ['./analytics.component.scss']
})
export class AnalyticsComponent implements OnInit {

  legend: boolean = true;
  showLabels: boolean = true;
  animations: boolean = true;
  xAxis: boolean = true;
  yAxis: boolean = true;
  showYAxisLabel: boolean = true;
  showXAxisLabel: boolean = true;
  xAxisLabel: string = 'Position';
  yAxisLabel: string = 'Points';
  timeline: boolean = true;
  view: [number, number] = [800, 500];
  legendPosition = LegendPosition.Right;


  LeagueData!: ILeagueData;
  ChartData: MultiSeries = [];

  update$: Subject<any> = new Subject();

  customColors = (value: string) => {
    console.log(value);
    return "#ff0000";
  }

  colorScheme  = {
    domain: ['#5AA454', '#E44D25', '#CFC0BB', '#7aa3e5', '#a8385d', '#aae3f5']
  };

  $Destroyed = new Subject();

  constructor(private dataService: DataService, private playerService: PlayerService) {
  }

  ngOnInit(): void {
    this.dataService.LeagueData$.pipe(takeUntil(this.$Destroyed)).subscribe(data => {
      this.LeagueData = data;
      this.buildData()
    });

  }

  onSelect(data: DataItem): void {
    console.log('Item clicked', JSON.parse(JSON.stringify(data)));
  }

  onActivate(data: DataItem): void {
    console.log('Activate', JSON.parse(JSON.stringify(data)));
  }

  onDeactivate(data: DataItem): void {
    console.log('Deactivate', JSON.parse(JSON.stringify(data)));
  }


  buildData(): void {
    const chartData: MultiSeries = []
    const ownerPlayers = this.LeagueData.Owners.map(o => this.playerService.buildPlayers(o, false));
    ownerPlayers.forEach(o => {

      let multi: Series = {
        name: o.Name,
        series: [
          {
            name: "QB",
            value: 0,
          },
          {
            name: "WR",
            value: 0,
          },
          {
            name: "RB",
            value: 0,
          },
          {
            name: "TE",
            value: 0,
          }
        ]
      };

      o.Players.forEach(p => {
        let dataItem = multi.series.find(s => s.name == p.Position.toUpperCase());
        if (!!dataItem) {
          dataItem.value += p.Score;
        }
      });

      chartData.push(multi);
      // this.customColors.push({
      //   name: o.Name,
      //   value: "#dcdcdc"
      // })
    });

    this.ChartData = [...chartData]
  }
}


