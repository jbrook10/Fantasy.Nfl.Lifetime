export type YearType = 'Regular' | 'Post';
export type PositionType = 'qb' | 'wr' | 'rb' | 'te';

export interface ILeagueData {
  Year: number;
  SeasonType: YearType;
  LastUpdated: string;
  Owners: IOwner[];
}

export interface IOwner {
  Name: string;
  Score: number;
  Players: IPlayer[];

  // client side only
  TotalScore: number;
}

export interface IPlayer {
  FantasyOwner: string;
  Name: string;
  Position: PositionType;
  Link: string;
  Age: number;

  G: number,
  PassYd: number,
  PassTd: number,
  RushYd: number,
  RushTd: number,
  Rec: number,
  RecYd: number,
  RecTd: number

  Score: number;

  // front end only
  Scoring: boolean;
}
