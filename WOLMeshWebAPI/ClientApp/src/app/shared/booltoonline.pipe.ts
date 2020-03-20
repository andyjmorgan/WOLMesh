import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'booltoonline'
})
export class BooltoonlinePipe implements PipeTransform {

  transform(value: boolean, args?: any): any {
    if (value) {
      return "Online";
    }
    else {
      return "Offline";
    }
  }

}
