import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'booltoresult'
})
export class BooltoresultPipe implements PipeTransform {

  transform(value: boolean, args?: any): any {
    if (value) {
      return "Success";
    }
    else {
      return "Failed";
    }
  }
}
