import { Component, OnInit } from '@angular/core';
import { Observable, map, shareReplay } from 'rxjs';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { YearType } from 'src/app/models/models';
import { DataService } from 'src/app/service/data.service';


@Component({
  selector: 'app-navigation',
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.scss']
})
export class NavigationComponent implements OnInit {

  Year = '';
  Years: number[] = [];
  YearType: YearType = 'Regular';


  isHandset$: Observable<boolean> = this.breakpointObserver.observe(Breakpoints.Handset)
    .pipe(
      map(result => result.matches),
      shareReplay()
    );


  constructor(private dataService: DataService, private breakpointObserver: BreakpointObserver) {
    const today = new Date();
    if (today.getMonth() < 8) {
      this.Year = today.getFullYear() - 1 + '';
    } else {
      this.Year = new Date().getFullYear() + '';
    }

    for (let index = 2019; index <= today.getFullYear(); index++) {
      this.Years.push(index)
    }
   }

  ngOnInit(): void {
    this.loadFile();
    this.YearType = this.dataService.YearType;
  }

  loadFile(): void {
    this.dataService.Year = +this.Year;
    this.dataService.YearType = this.YearType;
    this.dataService.GetLeagueData(+this.dataService.Year, this.dataService.YearType);
  }

}
