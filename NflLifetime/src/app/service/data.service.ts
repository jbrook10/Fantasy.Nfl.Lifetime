import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ReplaySubject } from 'rxjs';
import { ILeagueData, YearType } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class DataService {

  public LeagueData$ = new ReplaySubject<ILeagueData>(1);

  private year: number = new Date().getFullYear();
  public get Year(): number {
    return this.year;
  }
  public set Year(year: number) {
    this.year = year;
  }

  private yearType: YearType = 'Regular';
  public get YearType(): YearType {
    return this.yearType;
  }
  public set YearType(value: YearType) {
    this.yearType = value;
  }


  constructor(private http: HttpClient) { }

  public GetLeagueData(year: number, type: YearType): void {
    this.year = year;
    this.yearType = type;

    this.http.get<ILeagueData>(`assets/${this.year}.${this.yearType}.Data.json`).subscribe(d => {
      this.LeagueData$.next(d);
    });
  }

}
