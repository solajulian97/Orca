const predef = require('customindicators/predef');
const graphics = require('customindicators/graphics');

class AutoLegProfile {
    init() {
        this.completedLegs = [];
        this.currentLeg = null;
        this.extremePrice = 0;
        this.extremeIndex = 0;
        this.extremeTime = null;
        this.isUpLeg = true;
        this.lastVolume = 0;
        this.lastBarIndex = -1;
    }

    map(d, i, history) {
        const contract = history.contractInfo();
        const tickSize = contract ? contract.tickSize : 0.25;

        const close = d.close();
        const high = d.high();
        const low = d.low();
        const volume = d.volume();
        const time = d.timestamp();

        // Initialize first leg
        if (!this.currentLeg) {
            this.isUpLeg = true;
            this.extremePrice = high;
            this.extremeIndex = i;
            this.extremeTime = time;
            this.currentLeg = this.createNewLeg(i, time, close, high, low, true);
        }

        // Handle real-time volume vs historical
        let tickVol = volume;
        if (i === this.lastBarIndex) {
            tickVol = volume - this.lastVolume;
        }
        this.lastVolume = volume;
        this.lastBarIndex = i;

        // Update Extremes
        let foundNewExtreme = false;
        if (this.isUpLeg) {
            if (high >= this.extremePrice) {
                this.extremePrice = high;
                this.extremeIndex = i;
                this.extremeTime = time;
                foundNewExtreme = true;
            }
        } else {
            if (low <= this.extremePrice) {
                this.extremePrice = low;
                this.extremeIndex = i;
                this.extremeTime = time;
                foundNewExtreme = true;
            }
        }

        // Reversal Logic
        const threshold = this.props.reversalTicks * tickSize;
        if (!foundNewExtreme) {
            if (this.isUpLeg && (this.extremePrice - low) >= threshold) {
                this.doReversal(false, i, time, low, high, close, tickSize);
            } else if (!this.isUpLeg && (high - this.extremePrice) >= threshold) {
                this.doReversal(true, i, time, low, high, close, tickSize);
            }
        }

        // Accumulate Tick Data
        if (tickVol > 0) {
            this.updateLegData(this.currentLeg, close, tickVol, high, low, i, tickSize);
        }

        // Build Graphics
        const gItems = [];

        // Render current leg at the right edge
        if (this.currentLeg) {
            this.renderProfile(this.currentLeg, true, gItems, tickSize);
        }

        // Render past legs at their start times
        for (let j = 0; j < this.completedLegs.length; j++) {
            this.renderProfile(this.completedLegs[j], false, gItems, tickSize);
        }

        return {
            graphics: { items: gItems }
        };
    }

    createNewLeg(idx, t, p, h, l, isUp) {
        return {
            startIndex: idx,
            startTime: t,
            high: h,
            low: l,
            isUp: isUp,
            profile: {},
            maxVol: 0
        };
    }

    updateLegData(leg, price, vol, high, low, idx, tickSize) {
        leg.high = Math.max(leg.high, high);
        leg.low = Math.min(leg.low, low);
        leg.endIndex = idx;

        const comp = this.props.tickCompression * tickSize;
        const key = Math.round(price / comp) * comp;

        if (!leg.profile[key]) leg.profile[key] = 0;
        leg.profile[key] += vol;

        leg.maxVol = Math.max(leg.maxVol, leg.profile[key]);
    }

    doReversal(toUp, idx, t, low, high, close, tickSize) {
        const old = this.currentLeg;

        // Save old if it meets minimum size
        if ((old.high - old.low) / tickSize >= this.props.minLegTicks) {
            this.completedLegs.push(old);
            if (this.completedLegs.length > this.props.legsToDisplay) {
                this.completedLegs.shift();
            }
        }

        this.isUpLeg = toUp;
        this.currentLeg = this.createNewLeg(idx, t, close, high, low, toUp);
        this.extremePrice = toUp ? high : low;
        this.extremeIndex = idx;
        this.extremeTime = t;
        this.lastVolume = 0;
    }

    renderProfile(leg, isCurrent, items, tickSize) {
        const widthMax = isCurrent ? this.props.volWidth : this.props.pastWidth;
        const comp = this.props.tickCompression * tickSize;

        for (let pKey in leg.profile) {
            const price = parseFloat(pKey);
            const vol = leg.profile[pKey];
            const w = (vol / leg.maxVol) * widthMax;

            // X positioning: Tradovate uses px(positive) from left or px(negative) from right
            let xPos;
            if (isCurrent) {
                // Anchored to right edge
                xPos = graphics.op(graphics.px(-this.props.rightOffset), '-', graphics.px(w));
            } else {
                // Anchored to bar time
                xPos = graphics.du(leg.startTime);
            }

            items.push({
                tag: 'Rect',
                key: 'p' + leg.startTime + pKey,
                x: xPos,
                y: graphics.du(price),
                width: graphics.px(w),
                height: graphics.px(2), // Simple line-like rect
                color: this.props.color,
                fill: true
            });
        }
    }
}

module.exports = {
    name: "AutoLegProfile",
    description: "Dynamic volume profiles per price leg",
    calculator: AutoLegProfile,
    tags: ["Volume"],
    inputType: "bars",
    plotType: "overlay",
    params: {
        reversalTicks: predef.paramSpecs.number(20, 1, 1),
        minLegTicks: predef.paramSpecs.number(20, 1, 1),
        tickCompression: predef.paramSpecs.number(4, 1, 1),
        legsToDisplay: predef.paramSpecs.number(5, 1, 1),
        volWidth: predef.paramSpecs.number(150, 10, 1),
        pastWidth: predef.paramSpecs.number(60, 10, 1),
        rightOffset: predef.paramSpecs.number(60, 0, 1),
        color: predef.paramSpecs.color('rgba(65, 105, 225, 0.5)')
    }
};
