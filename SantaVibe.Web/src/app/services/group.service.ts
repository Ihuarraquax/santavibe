import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateGroupRequest, GroupResponse } from '../models/group.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class GroupService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  createGroup(request: CreateGroupRequest): Observable<GroupResponse> {
    return this.http.post<GroupResponse>(`${this.apiUrl}/group`, request);
  }
}
