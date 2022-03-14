

function _getTimeZoneOffsetInMs() {
    return new Date().getTimezoneOffset() * -60 * 1000;
}

export class DatesUtil {
    static ParseDayStringAsFullDate(day_string: string) {
        if (day_string.indexOf('T') < 0)
            day_string += "T00:00:00";
        return new Date(day_string);
    }
    static AddDays(base: Date, days_diff: number): Date {
        return new Date(base.setDate(base.getDate() + days_diff));
    }
    static NearestMidnight(after: Date) {
        return new Date(after.setHours(24, 0, 0, 0));
    }
    static formatMonth(date: Date) {
        return date.toLocaleDateString(undefined, { year: 'numeric', month: 'numeric' })
    }

    static DateToDatetimeInputString(timestamp: Date) {
        const date = new Date((timestamp.getTime() + _getTimeZoneOffsetInMs()));
        // slice(0, 19) includes seconds
        return date.toISOString().slice(0, 19);
    }
}