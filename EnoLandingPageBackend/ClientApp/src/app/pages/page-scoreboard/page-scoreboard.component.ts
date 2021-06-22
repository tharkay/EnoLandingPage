import { HttpClient } from '@angular/common/http';
import {
  Component,
  OnInit,
  ViewChild,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  TrackByFunction,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSort, SortDirection } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import {
  Scoreboard,
  ScoreboardInfoService,
} from 'projects/backend-api/src/lib';
import { ScoreboardService } from 'projects/backend-api/src/lib/model/scoreboardService';
import { ScoreboardTeam } from 'projects/backend-api/src/lib/model/scoreboardTeam';
import {
  DialogInfoComponent,
  InfoDialogData,
} from './dialog-info/dialog-info.component';
import { TeamDetailsMessage } from 'projects/backend-api/src/lib/model/teamDetailsMessage';
import { Observable } from 'rxjs';
import { AppState } from 'src/app/shared/states/App.state';
import { Select } from '@ngxs/store';

@Component({
  selector: 'app-page-scoreboard',
  templateUrl: './page-scoreboard.component.html',
  styleUrls: ['./page-scoreboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageScoreboardComponent implements OnInit {
  @Select(AppState.teamInfo)
  public teamInfo$!: Observable<TeamDetailsMessage>;

  public round: number = 0;
  public roundLength: number = 60;
  public isCurrentRound: boolean = false;
  public get displayedColumns(): string[] {
    return ['teamId', ...this.columns];
  }

  public services: ScoreboardService[] | undefined;

  public dataSource: MatTableDataSource<any> = new MatTableDataSource(
    []
  ) as any;

  public columns: string[] = [];

  public countDownConfig = {
    leftTime: 60,
    format: 'mm:ss',
  };

  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    public dialog: MatDialog,
    private _httpClient: HttpClient,
    private scoreboardInfoService: ScoreboardInfoService,
    private ref: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // this.ref.detach();
  }
  ngAfterViewInit() {
    this.loadRound();

    // this.scoreboardInfoService.apiScoreboardInfoScoreboardJsonGet();
    // this.scoreboardInfoService.apiScoreboardInfoScoreboardroundIdJsonGet(23);
  }

  public loadRound(round: number | null = null): void {
    let suffix: any = '';
    if (round !== null) {
      suffix = Math.max(round, 0);
    }

    this._httpClient
      .get<Scoreboard>('/api/scoreboardinfo/scoreboard' + suffix + '.json')
      .subscribe((scoreboard) => {
        this.round = scoreboard.currentRound!;
        this.roundLength = scoreboard.roundLength ?? 60;
        this.services =
          scoreboard.services?.sort((a, b) => a.serviceId! - b.serviceId!) ||
          [];

        console.log(this.services);

        let currentTime = new Date();
        let startTime = new Date(scoreboard.startTimestamp!);
        let endTime = new Date(scoreboard.endTimestamp!);
        const timeLeft =
          (endTime.getTime() +
            this.roundLength * 1000 -
            currentTime.getTime()) /
          1000;
        this.isCurrentRound = timeLeft >= 0;

        this.countDownConfig = {
          ...this.countDownConfig,
          leftTime: timeLeft,
        };

        this.dataSource.data =
          scoreboard.teams?.sort((lhs, rhs) => {
            return rhs.totalScore - lhs.totalScore;
          }).map((team) => {
            let row: any = {
              team: team,
            };
            team.serviceDetails?.forEach((service) => {
              row[service.serviceId!.toString()] = service;
            });
            return row;
          }) || [];

        this.columns =
          scoreboard.services?.reduce((accumulator, service) => {
            accumulator.push(service.serviceId!.toString());
            return accumulator;
          }, [] as string[]) || [];
        this.ref.markForCheck();

        if (this.isCurrentRound) {
          setTimeout(() => this.gotoCurrentRound(), (1.5 + timeLeft) * 1000);
        }
      });
  }

  public gotoFirstRound(): void {
    this.loadRound(0);
  }

  public gotoPreviousRound(): void {
    this.loadRound(this.round - 1);
  }

  public gotoNextRound(): void {
    // TODO: check if round exists somehow ???
    this.loadRound(this.round + 1);
  }

  public gotoCurrentRound(): void {
    this.loadRound();
  }

  public trackById: TrackByFunction<ScoreboardTeam> = (
    index: number,
    item: ScoreboardTeam
  ) => {
    return item.teamId;
  };

  openInfo(row: any, service: ScoreboardService) {
    let data: InfoDialogData = {
      row: row,
      service: service,
    };
    this.dialog.open(DialogInfoComponent, {
      data: data,
    });
  }
}
