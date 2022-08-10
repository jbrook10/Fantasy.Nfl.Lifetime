import { Injectable } from '@angular/core';
import { IOwner, IPlayer } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class PlayerService {

  constructor() { }

  public buildPlayers(owner: IOwner,  showAll: boolean): IOwner {

    const newOwner: IOwner = {
      Name: owner.Name,
      Score: owner.Score,
      Players: [],
      CountingScore: owner.CountingScore
    };

    const players: IPlayer[] = [];
    players.push(...this.AddPlayers(owner, 'qb', 1, showAll));
    players.push(...this.AddPlayers(owner, 'rb', 2, showAll));
    players.push(...this.AddPlayers(owner, 'wr', 3, showAll));
    players.push(...this.AddPlayers(owner, 'te', 1, showAll));

    newOwner.Players = players;
    return newOwner;
  }

  private AddPlayers(owner: IOwner, position: string, num: number, showAll: boolean): IPlayer[] {
    const players: IPlayer[] = [];

    const matchingPlayers = owner.Players.filter(p => p.Position == position);
    matchingPlayers.sort((a, b) => b.Score - a.Score);
    const topN = matchingPlayers.slice(0, num);
    topN.forEach(p => p.Scoring = true && showAll);

    if (showAll) {
      topN.push(...matchingPlayers.slice(num));
    }

    players.push(...topN);
    return players;
  }

}
