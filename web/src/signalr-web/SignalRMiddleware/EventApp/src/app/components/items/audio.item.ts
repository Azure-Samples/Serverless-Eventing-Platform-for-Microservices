export class AudioItem{
    id: string
    categoryId: string
    audioUrl: string
    transcript : string
    isActive: boolean = true;
    showNotification: boolean = false;

    constructor(e_id){
        this.id = e_id
    }

    getId(){
        return this.id
    }

    getCategoryId(){
        return this.categoryId
    }
    getAudioUrl(){
        return this.audioUrl
    }

    setCategoryId(id: string){
        this.id = id
    }
    setAudioUrl(url:string){
        this.audioUrl = url
    }

    getTranscript(){
        return this.transcript
    }
    setTranscript(e_transcript:string){
        this.transcript = e_transcript
    }
}