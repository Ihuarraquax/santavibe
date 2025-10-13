import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { CreateGroupRequest, Group } from '../models/group.model';

@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private readonly apiUrl = 'https://localhost:5001';
  private groups = signal<Group[]>([]);

  constructor(private http: HttpClient) {}

  createGroup(request: CreateGroupRequest): Observable<Group> {
    return this.http.post<Group>(`${this.apiUrl}/group`, request)
      .pipe(
        tap(group => {
          // Add the new group to the signal
          this.groups.update(groups => [...groups, group]);
        }),
        catchError(this.handleError)
      );
  }

  getGroups(): Observable<Group[]> {
    // TODO: Implement when GET endpoint is available
    return throwError(() => new Error('Not implemented'));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      if (error.status === 400 && error.error?.errors) {
        // Validation errors
        const validationErrors = Object.values(error.error.errors).flat();
        errorMessage = validationErrors.join(', ');
      } else {
        errorMessage = `Server returned code ${error.status}: ${error.error?.title || error.message}`;
      }
    }

    return throwError(() => new Error(errorMessage));
  }
}
