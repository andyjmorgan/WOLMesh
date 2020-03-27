import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent {
  public showAbout: boolean;
  public aboutLogo = require("./Assets/icon128.png");
  title = 'Wake On Lan Mesh';
}
